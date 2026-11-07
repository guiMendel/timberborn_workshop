using ChooseYourTrees.Core;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var a = new TreeLocation(Guid.NewGuid(), 1, 2, 3);
var replacement = new TreeLocation(Guid.NewGuid(), 1, 2, 3);
var b = new TreeLocation(Guid.NewGuid(), 4, 5, 6);
var store = new PriorityStore();
Check(store.Mode == PriorityMode.Tile && store.Get(a) == PriorityStore.Normal, "Default is normal tile priority.");
store.Set(a, 4);
Check(store.Get(replacement) == 4, "Tile priority survives tree replacement.");
store.Set(b, 0);
store.SwitchMode(PriorityMode.Tree, new[] { replacement });
Check(store.Get(replacement) == 4 && store.Get(b) == PriorityStore.Normal, "Conversion preserves occupied priority and discards empty tiles.");
store.ForgetTree(replacement.TreeId);
Check(store.Get(replacement) == PriorityStore.Normal, "Cutting clears tree priority.");
store.Set(replacement, 3);
store.SwitchMode(PriorityMode.Tile, new[] { replacement });
Check(store.Get(a) == 3, "Tree to tile conversion preserves priority.");
try { store.SwitchMode(PriorityMode.Tree, new[] { a, replacement }); throw new Exception("Duplicate tile accepted."); }
catch (InvalidOperationException) { }
Check(store.Mode == PriorityMode.Tile && store.Get(a) == 3, "Failed conversion must not lose state.");
try { store.Set(a, 5); throw new Exception("Invalid priority accepted."); }
catch (ArgumentOutOfRangeException) { }
var restored = new PriorityStore();
restored.Restore(store.Mode, store.TileEntries, store.TreeEntries);
Check(restored.Get(a) == 3, "Saved tile priority round-trips.");
try
{
    restored.Restore(PriorityMode.Tree, store.TileEntries, store.TreeEntries);
    throw new Exception("Invalid save accepted.");
}
catch (InvalidOperationException) { }
Check(restored.Mode == PriorityMode.Tile && restored.Get(a) == 3, "Failed restore must not change state.");
restored.Set(b, 1);
restored.SwitchMode(PriorityMode.Tree, new[] { a, b });
Check(restored.Get(a) == 3 && restored.Get(b) == 1, "Both occupied priorities convert independently.");
var restoredTrees = new PriorityStore();
restoredTrees.Restore(restored.Mode, restored.TileEntries, restored.TreeEntries);
Check(restoredTrees.Get(a) == 3 && restoredTrees.Get(b) == 1, "Saved tree priorities round-trip.");
restoredTrees.ForgetTree(a.TreeId);
Check(restoredTrees.Get(a) == PriorityStore.Normal && restoredTrees.Get(b) == 1, "Cutting affects only the selected tree.");
try
{
    restoredTrees.Restore(PriorityMode.Tree, Array.Empty<KeyValuePair<(int, int, int), int>>(),
        new[] { new KeyValuePair<Guid, int>(b.TreeId, 99) });
    throw new Exception("Invalid saved priority accepted.");
}
catch (ArgumentOutOfRangeException) { }
Check(restoredTrees.Get(b) == 1, "Invalid saved priority leaves existing values intact.");
Console.WriteLine("Priority store checks passed.");
