using System;
using System.Collections.Generic;

namespace ChooseYourTrees.Core;

internal enum PriorityMode { Tile, Tree }

internal readonly struct TreeLocation
{
    public TreeLocation(Guid treeId, int x, int y, int z)
    {
        if (treeId == Guid.Empty) throw new ArgumentException("A tree must have a persistent ID.", nameof(treeId));
        TreeId = treeId;
        Tile = (x, y, z);
    }

    public Guid TreeId { get; }
    public (int x, int y, int z) Tile { get; }
}

/// <summary>Save-scoped priorities. Conversion replaces state only after validating all tree locations.</summary>
internal sealed class PriorityStore
{
    public const int Normal = 2;
    private Dictionary<(int x, int y, int z), int> _tiles = new();
    private Dictionary<Guid, int> _trees = new();

    public PriorityMode Mode { get; private set; } = PriorityMode.Tile;
    public IEnumerable<KeyValuePair<(int x, int y, int z), int>> TileEntries => _tiles;
    public IEnumerable<KeyValuePair<Guid, int>> TreeEntries => _trees;

    public void Restore(PriorityMode mode, IEnumerable<KeyValuePair<(int x, int y, int z), int>> tiles,
        IEnumerable<KeyValuePair<Guid, int>> trees)
    {
        if (mode != PriorityMode.Tile && mode != PriorityMode.Tree)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown saved priority mode.");
        if (tiles == null) throw new ArgumentNullException(nameof(tiles));
        if (trees == null) throw new ArgumentNullException(nameof(trees));
        var restoredTiles = new Dictionary<(int, int, int), int>();
        var restoredTrees = new Dictionary<Guid, int>();
        foreach (var entry in tiles)
        {
            ValidatePriority(entry.Value);
            if (!restoredTiles.TryAdd(entry.Key, entry.Value))
                throw new InvalidOperationException("Cannot restore priorities: duplicate tile.");
        }
        foreach (var entry in trees)
        {
            ValidatePriority(entry.Value);
            if (entry.Key == Guid.Empty || !restoredTrees.TryAdd(entry.Key, entry.Value))
                throw new InvalidOperationException("Cannot restore priorities: invalid or duplicate tree ID.");
        }
        if ((mode == PriorityMode.Tile && restoredTrees.Count != 0) ||
            (mode == PriorityMode.Tree && restoredTiles.Count != 0))
            throw new InvalidOperationException("Cannot restore priorities: saved entries do not match the saved mode.");
        _tiles = restoredTiles;
        _trees = restoredTrees;
        Mode = mode;
    }

    public int Get(TreeLocation tree) => Mode == PriorityMode.Tile
        ? (_tiles.TryGetValue(tree.Tile, out int tileValue) ? tileValue : Normal)
        : (_trees.TryGetValue(tree.TreeId, out int treeValue) ? treeValue : Normal);

    public void Set(TreeLocation tree, int priority)
    {
        ValidatePriority(priority);
        if (Mode == PriorityMode.Tile) SetSparse(_tiles, tree.Tile, priority);
        else SetSparse(_trees, tree.TreeId, priority);
    }

    public void ForgetTree(Guid treeId)
    {
        if (Mode == PriorityMode.Tree) _trees.Remove(treeId);
    }

    public void SwitchMode(PriorityMode mode, IEnumerable<TreeLocation> currentTrees)
    {
        if (mode != PriorityMode.Tile && mode != PriorityMode.Tree)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown tree priority mode.");
        if (currentTrees == null) throw new ArgumentNullException(nameof(currentTrees));
        if (mode == Mode) return;

        var tiles = new Dictionary<(int, int, int), int>();
        var trees = new Dictionary<Guid, int>();
        var seenIds = new HashSet<Guid>();
        var seenTiles = new HashSet<(int, int, int)>();
        foreach (TreeLocation tree in currentTrees)
        {
            if (tree.TreeId == Guid.Empty || !seenIds.Add(tree.TreeId) || !seenTiles.Add(tree.Tile))
                throw new InvalidOperationException("Cannot convert tree priorities: tree identities or tile positions are invalid or duplicated.");
            int priority = Get(tree);
            if (mode == PriorityMode.Tile) SetSparse(tiles, tree.Tile, priority);
            else SetSparse(trees, tree.TreeId, priority);
        }
        _tiles = tiles;
        _trees = trees;
        Mode = mode;
    }

    private static void ValidatePriority(int priority)
    {
        if (priority < 0 || priority > 4)
            throw new ArgumentOutOfRangeException(nameof(priority), priority, "Tree priority must be between 0 and 4.");
    }

    private static void SetSparse<TKey>(Dictionary<TKey, int> entries, TKey key, int value) where TKey : notnull
    {
        if (value == Normal) entries.Remove(key);
        else entries[key] = value;
    }
}
