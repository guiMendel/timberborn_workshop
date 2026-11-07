using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Timberborn.AreaSelectionSystem;
using Timberborn.AreaSelectionSystemUI;
using Timberborn.BlockSystem;
using Timberborn.BuilderPrioritySystem;
using Timberborn.Cutting;
using Timberborn.Forestry;
using Timberborn.InventorySystem;
using Timberborn.Navigation;
using Timberborn.PrioritySystem;
using Timberborn.YielderFinding;
using Timberborn.Yielding;
using UnityEngine;

namespace ChooseYourTrees.Game;

internal static class GamePatches
{
    private const string HarmonyId = "ChooseYourTrees.Game";
    private static Harmony? _harmony;
    private static TreePriorityService? _priorities;
    private static FieldInfo? _priorityField;
    private static float _lastEmptyDecisionLog = -100;

    public static void Install(TreePriorityService priorities)
    {
        if (_harmony != null) throw new InvalidOperationException("Choose Your Trees patches are already installed.");
        var harmony = new Harmony(HarmonyId);
        try
        {
            Type lumberjack = AccessTools.TypeByName("Timberborn.Forestry.LumberjackFlagWorkplaceBehavior")
                ?? throw new MissingMemberException("Lumberjack behavior type was not found; the game version may be incompatible.");
            Type priorityTool = AccessTools.TypeByName("Timberborn.BuilderPrioritySystemUI.BuilderPriorityTool")
                ?? throw new MissingMemberException("Assign Priority tool type was not found; the game version may be incompatible.");
            _priorityField = AccessTools.Field(priorityTool, "_priority")
                ?? throw new MissingFieldException(priorityTool.FullName, "_priority");
            _priorities = priorities;
            MethodInfo findCuttable = AccessTools.Method(lumberjack, "FindCuttable")
                ?? throw new MissingMethodException(lumberjack.FullName, "FindCuttable");
            // Performance Plus can replace this method and skip later prefixes. Priorities must run first.
            var findPrefix = new HarmonyMethod(typeof(GamePatches), nameof(FindCuttablePrefix))
            {
                priority = HarmonyLib.Priority.First,
                before = new[] { "acalc79.PerformancePlus.LumberjackCandidates" }
            };
            harmony.Patch(findCuttable, prefix: findPrefix);
            MethodInfo cut = AccessTools.Method(typeof(Cuttable), "Cut")
                ?? throw new MissingMethodException(typeof(Cuttable).FullName, "Cut");
            harmony.Patch(cut, postfix: new HarmonyMethod(typeof(GamePatches), nameof(CutPostfix)));
            Patch(harmony, priorityTool, "ActionCallback", nameof(PriorityActionPrefix));
            Patch(harmony, priorityTool, "PreviewCallback", nameof(PriorityPreviewPrefix));
            MethodInfo processInput = AccessTools.Method(priorityTool, "ProcessInput")
                ?? throw new MissingMethodException(priorityTool.FullName, "ProcessInput");
            harmony.Patch(processInput, transpiler: new HarmonyMethod(typeof(GamePatches), nameof(PriorityPickerTranspiler)));
            _harmony = harmony;
            var lumberjackMethod = AccessTools.Method(lumberjack, "FindCuttable");
            var patchInfo = Harmony.GetPatchInfo(lumberjackMethod);
            Debug.Log("[Choose Your Trees] Installed lumberjack and Assign Priority hooks. Lumberjack prefixes: " +
                string.Join(", ", patchInfo.Prefixes.Select(p => p.owner + ":" + p.PatchMethod.DeclaringType?.FullName + "." + p.PatchMethod.Name)));
        }
        catch (Exception ex)
        {
            harmony.UnpatchAll(HarmonyId);
            _priorities = null;
            var failure = new InvalidOperationException("[Choose Your Trees] Could not install game hooks; the installed Timberborn version may be incompatible.", ex);
            Debug.LogException(failure);
            throw failure;
        }
    }

    public static void Uninstall()
    {
        _harmony?.UnpatchAll(HarmonyId);
        _harmony = null;
        _priorities = null;
        _priorityField = null;
    }

    private static void Patch(Harmony harmony, Type type, string methodName, string prefixName)
    {
        MethodInfo original = AccessTools.Method(type, methodName) ?? throw new MissingMethodException(type.FullName, methodName);
        harmony.Patch(original, prefix: new HarmonyMethod(typeof(GamePatches), prefixName));
    }

    // Let the game's own finder evaluate each tier, including accessibility, inventory and eligibility.
    private static bool FindCuttablePrefix(object __instance, int liftingCapacity, YielderFinder ____yielderFinder,
        TreeCuttingArea ____treeCuttingArea, Inventory ____inventory, Accessible ____accessible,
        YieldStatus ____yieldStatus, ref YielderSearchResult __result)
    {
        if (_priorities == null) return true;
        var all = ____treeCuttingArea.YieldersInArea.ToArray();
        var candidates = all.Where(y => !y.Reservable.Reserved).ToArray();
        var tiers = new string[5];
        for (int tier = 4; tier >= 0; tier--)
        {
            int desired = tier;
            var eligibleTier = candidates.Where(y =>
            {
                var tree = y.GetComponent<TreeComponent>();
                return tree != null && _priorities.GetPriority(tree) == desired;
            }).ToArray();
            YielderSearchResult result = ____yielderFinder.FindLivingYielderWithoutAccessible(____inventory, ____accessible, liftingCapacity, eligibleTier);
            tiers[tier] = $"{(Timberborn.PrioritySystem.Priority)tier}:{eligibleTier.Length}/{(result.HasYielder ? "selected" : result.NoYielderInRange ? "none-reachable-or-ready" : "no-capacity")}";
            if (!result.HasYielder) continue;
            ____yieldStatus.UpdateStatus(result);
            __result = result;
            LogDecision(__instance, all, ____accessible, liftingCapacity, tiers, result.Yielder);
            return false;
        }
        if (Time.realtimeSinceStartup - _lastEmptyDecisionLog >= 10f)
        {
            _lastEmptyDecisionLog = Time.realtimeSinceStartup;
            LogDecision(__instance, all, ____accessible, liftingCapacity, tiers, null);
        }
        // Preserve the vanilla status result when no eligible tree was found.
        return true;
    }

    private static void LogDecision(object workplace, Yielder[] all, Accessible accessible, int capacity, string[] tiers, Yielder? chosen)
    {
        try
        {
            var lines = new StringBuilder();
            foreach (var yielder in all)
            {
                var tree = yielder.GetComponent<TreeComponent>();
                if (tree == null) continue;
                int priority = _priorities!.GetPriority(tree);
                if (priority == 2 && yielder != chosen) continue;
                bool reachable = accessible.FindTerrainPath(yielder.CenterPosition, out var distance);
                if (lines.Length > 4000) { lines.Append(" ...truncated"); break; }
                lines.Append($" [tree={yielder.Coordinates} priority={(Timberborn.PrioritySystem.Priority)priority}" +
                    $" reserved={yielder.Reservable.Reserved} yielding={yielder.IsYielding} reachable={reachable} distance={distance}]");
            }
            var location = (workplace as Timberborn.BaseComponentSystem.BaseComponent)?.GetComponent<BlockObject>()?.Coordinates;
            Debug.Log($"[Choose Your Trees] Lumberjack decision workplace={location} capacity={capacity} " +
                $"markedTrees={all.Length} available={all.Count(y => !y.Reservable.Reserved)} " +
                $"tiers={string.Join(",", tiers.Where(t => t != null))} " +
                $"chosen={(chosen != null ? chosen.Coordinates.ToString() : "none; using vanilla status")}.{lines}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Choose Your Trees] Failed to collect lumberjack decision diagnostics: " + ex);
        }
    }

    private static void CutPostfix(Cuttable __instance) => _priorities?.OnTreeCut(__instance);

    private static void PriorityActionPrefix(object __instance, IEnumerable<BlockObject> blockObjects)
    {
        if (_priorities == null || _priorityField == null) return;
        var priority = (Timberborn.PrioritySystem.Priority)_priorityField.GetValue(__instance);
        int changed = 0;
        foreach (BlockObject block in blockObjects)
        {
            var tree = block.GetComponent<TreeComponent>();
            if (tree == null) continue;
            _priorities.SetPriority(tree, (int)priority, changed < 20 ? "priority tool" : null);
            changed++;
        }
        if (changed > 0) Debug.Log($"[Choose Your Trees] Assigned priority {priority} to {changed} trees using the priority tool; details logged for up to 20 trees.");
    }

    private static bool PriorityPreviewPrefix(object __instance, IEnumerable<BlockObject> blockObjects,
        Vector3Int start, Vector3Int end, bool selectionStarted, bool selectingArea)
    {
        var selected = blockObjects.Where(block => block.GetComponent<TreeComponent>() != null ||
            (block.GetComponent<BuilderPrioritizable>()?.Enabled ?? false));
        string fieldName = (selectionStarted || selectingArea) ? "_actionSelectionDrawer" : "_highlightSelectionDrawer";
        var drawer = (BlockObjectSelectionDrawer)AccessTools.Field(__instance.GetType(), fieldName).GetValue(__instance);
        drawer.Draw(selected, start, end, selectingArea);
        return false;
    }

    // Pick all block objects, then let each callback independently select trees or construction sites.
    private static IEnumerable<CodeInstruction> PriorityPickerTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        int replacements = 0;
        foreach (var instruction in instructions)
        {
            if (instruction.operand is MethodInfo method && method.IsGenericMethod &&
                method.DeclaringType == typeof(AreaBlockObjectPicker) && method.Name == "PickBlockObjects" &&
                method.GetGenericArguments()[0] == typeof(BuilderPrioritizable))
            {
                instruction.operand = method.GetGenericMethodDefinition().MakeGenericMethod(typeof(BlockObject));
                replacements++;
            }
            yield return instruction;
        }
        if (replacements != 1)
            throw new InvalidOperationException($"Expected one priority-tool picker call, found {replacements}; the game version may be incompatible.");
    }
}
