using System;
using System.Collections.Generic;
using System.Linq;
using ChooseYourTrees.Core;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;

using Timberborn.WorldPersistence;
using UnityEngine;

namespace ChooseYourTrees.Game;

internal sealed class PriorityDataException : Exception
{
    public PriorityDataException(string operation, Exception cause)
        : base($"Choose Your Trees could not {operation} priority data. The original data was not intentionally replaced; inspect the inner exception and game log.", cause) { }
}

/// <summary>Persists each save's active priority mode and its sparse entries.</summary>
internal sealed class PriorityPersistence : ILoadableSingleton, ISaveableSingleton
{
    private static readonly SingletonKey Key = new("ChooseYourTrees.PriorityData");
    private static readonly PropertyKey<int> ModeKey = new("Mode");
    private static readonly ListKey<Vector3Int> TilesKey = new("Tiles");
    private static readonly ListKey<int> TilePrioritiesKey = new("TilePriorities");
    private static readonly ListKey<Guid> TreesKey = new("Trees");
    private static readonly ListKey<int> TreePrioritiesKey = new("TreePriorities");
    private readonly ISingletonLoader _loader;
    public PriorityStore Store { get; } = new();

    public PriorityPersistence(ISingletonLoader loader) => _loader = loader;

    public void Load()
    {
        if (!_loader.TryGetSingleton(Key, out var saved))
        {
            Debug.Log("[Choose Your Trees] No priority data in this save; starting with normal tile priorities.");
            return;
        }
        try
        {
            var tiles = saved.Get(TilesKey);
            var tileValues = saved.Get(TilePrioritiesKey);
            var trees = saved.Get(TreesKey);
            var treeValues = saved.Get(TreePrioritiesKey);
            if (tiles.Count != tileValues.Count || trees.Count != treeValues.Count)
                throw new InvalidOperationException("Saved priority keys and values have different lengths.");
            var tileEntries = tiles.Select((tile, i) => new KeyValuePair<(int, int, int), int>((tile.x, tile.y, tile.z), tileValues[i])).ToArray();
            var treeEntries = trees.Select((id, i) => new KeyValuePair<Guid, int>(id, treeValues[i])).ToArray();
            Store.Restore((PriorityMode)saved.Get(ModeKey), tileEntries, treeEntries);
            Debug.Log($"[Choose Your Trees] Loaded {tiles.Count} tile and {trees.Count} tree priorities in {Store.Mode} mode.");
        }
        catch (Exception ex)
        {
            var failure = new PriorityDataException("load", ex);
            Debug.LogException(failure);
            throw failure;
        }
    }

    public void Save(ISingletonSaver saver)
    {
        try
        {
            var tileEntries = Store.TileEntries.ToArray();
            var treeEntries = Store.TreeEntries.ToArray();
            var saved = saver.GetSingleton(Key);
            saved.Set(ModeKey, (int)Store.Mode);
            saved.Set(TilesKey, tileEntries.Select(entry => new Vector3Int(entry.Key.x, entry.Key.y, entry.Key.z)).ToArray());
            saved.Set(TilePrioritiesKey, tileEntries.Select(entry => entry.Value).ToArray());
            saved.Set(TreesKey, treeEntries.Select(entry => entry.Key).ToArray());
            saved.Set(TreePrioritiesKey, treeEntries.Select(entry => entry.Value).ToArray());
        }
        catch (Exception ex)
        {
            var failure = new PriorityDataException("save", ex);
            Debug.LogException(failure);
            throw failure;
        }
    }
}
