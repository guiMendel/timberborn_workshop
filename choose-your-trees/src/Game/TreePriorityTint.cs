using System.Collections.Generic;
using Timberborn.Forestry;
using UnityEngine;

namespace ChooseYourTrees.Game;

/// <summary>Applies a reversible emission tint without adding the game's selection/construction overlay.</summary>
internal sealed class TreePriorityTint
{
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private const float Strength = 0.42f;
    private readonly Dictionary<TreeComponent, List<RendererTint>> _trees = new();

    public int TreeCount => _trees.Count;
    public int RendererCount { get; private set; }
    public int UnsupportedMaterials { get; private set; }
    public string FirstMaterialInfo { get; private set; } = "none";
    public string FirstSupportedInfo { get; private set; } = "none";

    public void Apply(TreeComponent tree, Color priorityColor)
    {
        if (!_trees.TryGetValue(tree, out var renderers))
        {
            renderers = Capture(tree);
            _trees.Add(tree, renderers);
        }
        foreach (var entry in renderers) entry.Apply(priorityColor);
    }

    public string Describe(TreeComponent tree)
    {
        if (!_trees.TryGetValue(tree, out var renderers)) return "not tinted";
        return renderers.Count == 0 ? "no supported renderers" : $"slots={renderers.Count} {renderers[0].Describe()}";
    }

    public void Remove(TreeComponent tree)
    {
        if (!_trees.TryGetValue(tree, out var renderers)) return;
        foreach (var entry in renderers) entry.Restore();
        _trees.Remove(tree);
    }

    public void Clear()
    {
        foreach (var renderers in _trees.Values)
            foreach (var entry in renderers) entry.Restore();
        _trees.Clear();
        RendererCount = 0;
        UnsupportedMaterials = 0;
        FirstMaterialInfo = "none";
        FirstSupportedInfo = "none";
    }

    private List<RendererTint> Capture(TreeComponent tree)
    {
        var result = new List<RendererTint>();
        foreach (var renderer in tree.GameObject.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer == null) continue;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null) continue;
                bool hasEmission = material.HasProperty(EmissionColor);
                if (FirstMaterialInfo == "none")
                    FirstMaterialInfo = $"renderer={renderer.name} material={material.name} shader={material.shader?.name} emission={hasEmission} lighting={material.HasProperty("_LightingColor")}";
                if (!hasEmission) { UnsupportedMaterials++; continue; }
                if (FirstSupportedInfo == "none") FirstSupportedInfo = $"renderer={renderer.name} material={material.name} shader={material.shader?.name} baseEmission={material.GetColor(EmissionColor)}";
                result.Add(new RendererTint(renderer, i, material));
                RendererCount++;
            }
        }
        return result;
    }

    private sealed class RendererTint
    {
        private readonly MeshRenderer _renderer;
        private readonly int _materialIndex;
        private readonly MaterialPropertyBlock _original = new();
        private readonly bool _hadMaterialBlock;
        private readonly Color _baseEmission;

        public RendererTint(MeshRenderer renderer, int materialIndex, Material material)
        {
            _renderer = renderer;
            _materialIndex = materialIndex;
            renderer.GetPropertyBlock(_original, materialIndex);
            _hadMaterialBlock = !_original.isEmpty;
            var effective = new MaterialPropertyBlock();
            if (_hadMaterialBlock) renderer.GetPropertyBlock(effective, materialIndex);
            else renderer.GetPropertyBlock(effective);
            _baseEmission = effective.HasColor(EmissionColor) ? effective.GetColor(EmissionColor) : material.GetColor(EmissionColor);
        }

        public void Apply(Color priorityColor)
        {
            if (_renderer == null) return;
            var block = new MaterialPropertyBlock();
            // Keep existing per-material settings; if there were none, copy the renderer-wide block.
            _renderer.GetPropertyBlock(block, _materialIndex);
            if (block.isEmpty) _renderer.GetPropertyBlock(block);
            Color tint = new(priorityColor.r * Strength, priorityColor.g * Strength, priorityColor.b * Strength, 1f);
            Color combined = new(_baseEmission.r + tint.r, _baseEmission.g + tint.g, _baseEmission.b + tint.b,
                Mathf.Max(_baseEmission.a, tint.a));
            block.SetColor(EmissionColor, combined);
            _renderer.SetPropertyBlock(block, _materialIndex);
        }

        public string Describe()
        {
            if (_renderer == null) return "renderer destroyed";
            var block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block, _materialIndex);
            return $"renderer={_renderer.name} slot={_materialIndex} base={_baseEmission} applied={block.GetColor(EmissionColor)}";
        }

        public void Restore()
        {
            if (_renderer != null) _renderer.SetPropertyBlock(_hadMaterialBlock ? _original : null, _materialIndex);
        }
    }
}
