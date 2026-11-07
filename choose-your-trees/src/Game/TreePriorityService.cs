using System;
using System.Collections.Generic;
using System.Linq;
using ChooseYourTrees.Core;
using Timberborn.BlockSystem;
using Timberborn.Cutting;
using Timberborn.EntitySystem;
using Timberborn.Forestry;
using Timberborn.PrioritySystem;
using Timberborn.PrioritySystemUI;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace ChooseYourTrees.Game;

internal sealed class TreePriorityService : IPostLoadableSingleton, IUnloadableSingleton
{
    private readonly PriorityStore _store;
    private readonly TreeModSettings _settings;
    private readonly EntityRegistry _entities;
    private readonly EventBus _events;
    private readonly TreeCuttingArea _cuttingArea;
    private readonly PriorityColors _colors;
    private readonly TreePriorityTint _tint = new();
    private readonly HashSet<TreeComponent> _tintedTrees = new();
    private bool _showPriorities;

    public TreePriorityService(PriorityPersistence persistence, TreeModSettings settings, EntityRegistry entities,
        EventBus events, TreeCuttingArea cuttingArea, PriorityColors colors)
    {
        _store = persistence.Store;
        _settings = settings;
        _entities = entities;
        _events = events;
        _cuttingArea = cuttingArea;
        _colors = colors;
    }

    public void PostLoad()
    {
        _events.Register(this);
        _settings.ApplyToTiles.ValueChanged += OnModeChanged;
        ConvertMode(_settings.ApplyToTiles.Value);
        GamePatches.Install(this);
    }

    public void Unload()
    {
        HidePriorities();
        GamePatches.Uninstall();
        _settings.ApplyToTiles.ValueChanged -= OnModeChanged;
        _events.Unregister(this);
    }

    public int GetPriority(TreeComponent tree) => _store.Get(Location(tree));

    public void SetPriority(TreeComponent tree, int priority, string? source = null)
    {
        var position = tree.GetComponent<BlockObject>().Coordinates;
        int oldPriority = GetPriority(tree);
        _store.Set(Location(tree), priority);
        if (_showPriorities) TintTree(tree);
        if (source != null)
        {
            var yielder = tree.GetComponent<Cuttable>().Yielder;
            Debug.Log($"[Choose Your Trees] Priority edit source={source} tree={position} id={Location(tree).TreeId} " +
                $"mode={_store.Mode} old={(Priority)oldPriority} new={(Priority)priority} " +
                $"marked={_cuttingArea.IsInCuttingArea(position)} reserved={yielder.Reservable.Reserved} yielding={yielder.IsYielding} " +
                $"overlay={(_showPriorities ? _tint.Describe(tree) : "inactive")}.");
        }
    }

    public void ShowPriorities()
    {
        if (_showPriorities) return;
        _showPriorities = true;
        int count = 0;
        var tiers = new int[5];
        foreach (var entity in _entities.Entities)
        {
            var tree = entity.GetComponent<TreeComponent>();
            if (tree == null || !IsUncutTree(tree)) continue;
            TintTree(tree);
            tiers[GetPriority(tree)]++;
            count++;
        }
        Debug.Log($"[Choose Your Trees] Priority tool opened; trees={count} tinted-material-slots={_tint.RendererCount} " +
            $"unsupported-material-slots={_tint.UnsupportedMaterials} tiers=[{string.Join(",", tiers)}]; " +
            $"first-material: {_tint.FirstMaterialInfo}; first-supported: {_tint.FirstSupportedInfo}.");
    }

    public void HidePriorities()
    {
        if (!_showPriorities) return;
        _showPriorities = false;
        _tint.Clear();
        Debug.Log($"[Choose Your Trees] Priority tool closed; restored {_tintedTrees.Count} tree colors.");
        _tintedTrees.Clear();
    }

    private void TintTree(TreeComponent tree)
    {
        // The vegetation shader uses _EmissionColor; a low emission retains the original model and texture.
        Color color = _colors.GetButtonColor((Priority)GetPriority(tree));
        _tint.Apply(tree, color);
        _tintedTrees.Add(tree);
    }

    [OnEvent]
    public void OnEntityInitialized(EntityInitializedEvent initialized)
    {
        var tree = initialized.Entity.GetComponent<TreeComponent>();
        if (_showPriorities && tree != null && IsUncutTree(tree)) TintTree(tree);
    }

    [OnEvent]
    public void OnEntityDeleted(EntityDeletedEvent deleted)
    {
        var tree = deleted.Entity.GetComponent<TreeComponent>();
        if (tree == null) return;
        _tint.Remove(tree);
        _tintedTrees.Remove(tree);
        _store.ForgetTree(deleted.Entity.EntityId);
    }

    public void OnTreeCut(Cuttable cuttable)
    {
        var tree = cuttable.GetComponent<TreeComponent>();
        if (tree == null) return;
        var location = Location(tree);
        _store.ForgetTree(location.TreeId);
        if (_tintedTrees.Remove(tree)) _tint.Remove(tree);
        Debug.Log($"[Choose Your Trees] Tree cut at {cuttable.Yielder.Coordinates}; mode={_store.Mode}, tree-specific priority cleared if applicable.");
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
            if (_showPriorities) foreach (var tree in _tintedTrees.ToArray()) if (tree != null) TintTree(tree);
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
