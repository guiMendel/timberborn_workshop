# Choose Your Trees development toolchain

Checked and installed 2026-11-07 on macOS 26.6, Apple Silicon (`osx-arm64`). Nothing was installed system-wide, and Unity or the game was not modified or launched.

## Tools

| Tool | Version | Executable |
| --- | --- | --- |
| .NET SDK | 10.0.403 (runtime 10.0.14) | `/Users/guimendel/.dotnet/dotnet` |
| ILSpy command-line decompiler (`ilspycmd`) | 11.2.0.10050 | `/Users/guimendel/.dotnet/tools/ilspycmd` |

The SDK was installed with Microsoft's `dotnet-install.sh` into `/Users/guimendel/.dotnet`. ILSpy was installed as a user-local .NET tool into `/Users/guimendel/.dotnet/tools`. These directories are outside the repository; no PATH or shell startup file was changed. Invoke directly, or set `DOTNET_ROOT="$HOME/.dotnet"` and add both directories to PATH for a shell session.

Example commands:

```sh
DOTNET_ROOT="$HOME/.dotnet" "$HOME/.dotnet/dotnet" --info
DOTNET_ROOT="$HOME/.dotnet" "$HOME/.dotnet/tools/ilspycmd" --version
DOTNET_ROOT="$HOME/.dotnet" "$HOME/.dotnet/tools/ilspycmd" \
  -t Timberborn.Forestry.LumberjackFlagWorkplaceBehavior \
  "$HOME/Library/Application Support/Steam/steamapps/common/Timberborn/Timberborn.app/Contents/Resources/Data/Managed/Timberborn.Forestry.dll"
```

## Compatibility check

ILSpy 11.2.0.10050 ran on the installed arm64 .NET runtime and decompiled `Timberborn.Forestry.LumberjackFlagWorkplaceBehavior` from this game's managed assemblies. Its IL output shows references to the `[netstandard]` facade. A temporary scratch class library targeting `netstandard2.1`, referencing this installation's `Timberborn.Forestry.dll` and `UnityEngine.CoreModule.dll`, built successfully with SDK 10.0.403 (0 warnings, 0 errors). The scratch project was outside the repository and was not retained.

This confirms that the installed tools can inspect and compile against those game assemblies using the `netstandard2.1` reference surface. It does not prove the mod loader's accepted output target, Harmony compatibility, runtime behavior, or Unity Editor compatibility. Confirm the loader and dependency requirements before creating the actual mod project. No Unity Editor/Hub is installed; this setup only supports code work and managed assembly inspection.
