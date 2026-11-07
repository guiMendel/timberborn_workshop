using System;
using System.Linq;
using ChooseYourTrees.Core;
using Timberborn.BlockSystem;
using Timberborn.Cutting;
using Timberborn.EntitySystem;
using Timberborn.Forestry;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace ChooseYourTrees.Game;

internal sealed class TreePriorityService : IPostLoadableSingleton, IUnloadableSingleton
{
    private readonly PriorityStore _store;
    private readonly TreeModSettings _settings;
    private readonly EntityRegistry _entities;

    public TreePriorityService(PriorityPersistence persistence, TreeModSettings settings, EntityRegistry entities)
    {
        _store = persistence.Store;
        _settings = settings;
        _entities = entities;
    }

    public void PostLoad()
    {
        _settings.ApplyToTiles.ValueChanged += OnModeChanged;
        ConvertMode(_settings.ApplyToTiles.Value);
        GamePatches.Install(this);
    }

    public void Unload()
    {
        GamePatches.Uninstall();
        _settings.ApplyToTiles.ValueChanged -= OnModeChanged;
    }

    public int GetPriority(TreeComponent tree) => _store.Get(Location(tree));
    public void SetPriority(TreeComponent tree, int priority) => _store.Set(Location(tree), priority);

    public void OnTreeCut(Cuttable cuttable)
    {
        var tree = cuttable.GetComponent<TreeComponent>();
        if (tree != null) _store.ForgetTree(tree.GetComponent<EntityComponent>().EntityId);
    }

    private void OnModeChanged(object sender, bool applyToTiles) => ConvertMode(applyToTiles);

    private void ConvertMode(bool applyToTiles)
    {
        PriorityMode target = applyToTiles ? PriorityMode.Tile : PriorityMode.Tree;
        if (_store.Mode == target) return;
        try
        {
            var trees = _entities.Entities.Select(entity => entity.GetComponent<TreeComponent>())
                .Where(tree => tree != null && IsUncutTree(tree)).Select(Location).ToArray();
            _store.SwitchMode(target, trees);
            Debug.Log($"[Choose Your Trees] Converted priorities to {target} mode for {trees.Length} existing trees.");
        }
        catch (Exception ex)
        {
            var failure = new PriorityDataException($"convert priorities to {target} mode", ex);
            Debug.LogException(failure);
            throw failure;
        }
    }

    private static bool IsUncutTree(TreeComponent tree)
    {
        var cuttable = tree.GetComponent<Cuttable>();
        return cuttable != null && !cuttable.Yielder.IsYieldRemoved;
    }

    private static TreeLocation Location(TreeComponent tree)
    {
        var id = tree.GetComponent<EntityComponent>().EntityId;
        var position = tree.GetComponent<BlockObject>().Coordinates;
        return new TreeLocation(id, position.x, position.y, position.z);
    }
}
