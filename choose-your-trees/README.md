# Choose Your Trees

Work-in-progress implementation of the mod described in [REQUIREMENTS.md](REQUIREMENTS.md) and [PLAN.md](PLAN.md). No gameplay implementation is present yet.

## Compatibility baseline

- Game build observed on this machine: `1.1.2.8-ffc2de0-sm` (Steam app `1062090`, build `26072923`). The local manifest declares game version `1.1.2.8`; confirm the Steam branch and update the declaration before release.
- Unity runtime observed in the plan: `6000.5.11f1`.
- Required mod IDs and minimum versions: `Harmony` `2.4.1`; `eMka.ModSettings` `1.1.1.0`.
- The installed Harmony manifest identifies `Harmony` version `2.4.1`; its assembly is `0Harmony.dll`.
- The installed Mod Settings 1.1 manifest identifies `eMka.ModSettings` version `1.1.1.0`; its core API assembly is `ModSettings.Core.dll`.

The requirements need Mod Settings for the tile/tree mode selector. Harmony is the planned patching dependency. Keep both declarations unless API research changes those decisions.

## Build setup

Use a .NET SDK compatible with the `netstandard2.1` target. On the setup machine, .NET SDK 10.0.403 is installed at `$HOME/.dotnet/dotnet` but is not on `PATH`; the scaffold was built successfully with it. Configure the three reference paths as MSBuild properties; no game or mod binaries are copied into source control:

```sh
DOTNET_ROOT="$HOME/.dotnet" "$HOME/.dotnet/dotnet" build ChooseYourTrees.csproj \
  -p:TimberbornManagedPath="/path/to/Timberborn.app/Contents/Resources/Data/Managed" \
  -p:HarmonyAssemblyPath="/path/to/Harmony/0Harmony.dll" \
  -p:ModSettingsCoreAssemblyPath="/path/to/ModSettings/Scripts/ModSettings.Core.dll"
```

The build output is staged under `dist/` with the manifest at its root, the mod assembly under `Scripts/`, and translations under `Localizations/`. Do not copy the referenced game or dependency assemblies into the package. The installed local paths observed during setup are listed in [TOOLCHAIN.md](TOOLCHAIN.md) when available; paths above are intentionally supplied per machine.

## Current state

The project builds and has focused checks for priority storage, conversion and restore. In-game testing confirmed the panel styling and priority-based lumberjack selection, including with Performance Plus enabled. The previous priority tint was invisible because vegetation shaders do not use the lighting-color input. The current emission-based tint and material restoration still need an in-game retest. Do not consider the package ready for normal saves yet.

Diagnostic messages in the game log use the prefix `[Choose Your Trees]`. Priority edits include tree coordinates, cutting designation and reservation; lumberjack decisions include priority tiers, candidate reachability and the selected tree; priority-tool logs include renderer counts, shader names and emission values. Performance Plus's lumberjack optimization is bypassed when this mod selects an eligible tree.

A tree's removal work is timed on the worker rather than exposed as durable partial chopping on the tree. An accurate chopping progress bar is not yet implemented; investigate live work and interruptions before deciding whether one can be shown reliably.

Run the isolated state checks with:

```sh
DOTNET_ROOT="$HOME/.dotnet" "$HOME/.dotnet/dotnet" run --project tests/PriorityStoreChecks.csproj
```

Use a separate test save when beginning in-game verification. Review the [plan](PLAN.md) for the remaining acceptance checks.
