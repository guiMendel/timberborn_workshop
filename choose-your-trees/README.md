# Choose Your Trees

Project scaffolding for the mod described in [REQUIREMENTS.md](REQUIREMENTS.md) and [PLAN.md](PLAN.md). No gameplay implementation is present yet.

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

The build output is staged under `dist/` with the manifest at its root and the mod assembly under `Scripts/`. Do not copy the referenced game or dependency assemblies into the package. The installed local paths observed during setup are listed in [TOOLCHAIN.md](TOOLCHAIN.md) when available; paths above are intentionally supplied per machine.

## Current state

This is only project infrastructure. There is no gameplay source or runtime behavior yet. Exact API contracts, hooks, UI behavior, persistence semantics, dependency API usage, and chopping progress remain to be investigated as set out in the plan. The empty scaffold builds and stages its assembly, but it has not been loaded or tested in-game.
