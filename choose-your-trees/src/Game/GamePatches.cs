using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Timberborn.AreaSelectionSystem;
using Timberborn.AreaSelectionSystemUI;
using Timberborn.BlockSystem;
using Timberborn.BuilderPrioritySystem;
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
            Patch(harmony, lumberjack, "FindCuttable", nameof(FindCuttablePrefix));
            Patch(harmony, priorityTool, "ActionCallback", nameof(PriorityActionPrefix));
            Patch(harmony, priorityTool, "PreviewCallback", nameof(PriorityPreviewPrefix));
            MethodInfo processInput = AccessTools.Method(priorityTool, "ProcessInput")
                ?? throw new MissingMethodException(priorityTool.FullName, "ProcessInput");
            harmony.Patch(processInput, transpiler: new HarmonyMethod(typeof(GamePatches), nameof(PriorityPickerTranspiler)));
            _harmony = harmony;
            Debug.Log("[Choose Your Trees] Installed lumberjack and Assign Priority hooks.");
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
    private static bool FindCuttablePrefix(int liftingCapacity, YielderFinder ____yielderFinder,
        TreeCuttingArea ____treeCuttingArea, Inventory ____inventory, Accessible ____accessible,
        YieldStatus ____yieldStatus, ref YielderSearchResult __result)
    {
        if (_priorities == null) return true;
        var candidates = ____treeCuttingArea.YieldersInArea.Where(y => !y.Reservable.Reserved).ToArray();
        for (int tier = 4; tier >= 0; tier--)
        {
            int desired = tier;
            var eligibleTier = candidates.Where(y =>
            {
                var tree = y.GetComponent<TreeComponent>();
                return tree != null && _priorities.GetPriority(tree) == desired;
            });
            YielderSearchResult result = ____yielderFinder.FindLivingYielderWithoutAccessible(____inventory, ____accessible, liftingCapacity, eligibleTier);
            if (!result.HasYielder) continue;
            ____yieldStatus.UpdateStatus(result);
            __result = result;
            return false;
        }
        // Preserve the vanilla status result when no eligible tree was found.
        return true;
    }

    private static void PriorityActionPrefix(object __instance, IEnumerable<BlockObject> blockObjects)
    {
        if (_priorities == null || _priorityField == null) return;
        var priority = (Timberborn.PrioritySystem.Priority)_priorityField.GetValue(__instance);
        foreach (BlockObject block in blockObjects)
        {
            var tree = block.GetComponent<TreeComponent>();
            if (tree != null) _priorities.SetPriority(tree, (int)priority);
        }
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
