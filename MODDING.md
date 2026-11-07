# Timberborn modding reference

Checked 2026-11-07. Start with the [official modding wiki](https://github.com/mechanistry/timberborn-modding/wiki) and [official tools and examples](https://github.com/mechanistry/timberborn-modding). Check documentation and dependencies against the exact game version and branch you are targeting.

## Getting started

1. Choose the smallest supported approach. The official pipeline supports data changes through JSON, translations and code; custom assets and more involved projects can use the Unity tools. See the [official overview and examples](https://github.com/mechanistry/timberborn-modding) and [creator guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3262168856).
2. Give the mod its own folder and `manifest.json`. Declare required and optional dependencies and follow the current [manifest documentation](https://github.com/mechanistry/timberborn-modding/wiki). Dependencies must load before the mods that use them.
3. For Unity work, use the editor version specified by the official project and follow its current setup and import instructions. The tools import game DLLs and assets and build the correct mod folder. A community maintainer also recommends reimporting version-specific assets after game updates; see their [setup notes](https://github.com/ihsoft/TimberbornMods/blob/main/docs/timberborn-new-repository-bootstrap-for-ai-agents.md).
4. Test the built mod in the game with its dependencies enabled. The built-in manager can open the local Mods folder, order dependencies and upload to Steam Workshop. Restart after mod changes. See [mod management](https://github.com/mechanistry/timberborn-modding/wiki/Mod-management) and the [official player and creator guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3262168856).
5. Test a fresh game and relevant save/load behavior, then test with the combinations the mod claims to support. Publish compatibility for the actual game branch and add required dependencies both to the manifest and the distribution listing.

The built-in pipeline arrived in Update 6. Old BepInEx/Thunderstore setup instructions describe a different, legacy workflow; do not assume they apply to current mods. [Community legacy guide](https://github.com/GinFuyou/Timberborn-GinFuyou/blob/main/LinuxBepInEx/README.md).

## Libraries for mod authors

These are optional community projects, not components every mod needs. Check each release's game compatibility and its current dependency instructions before using it.

| Project | What it provides | Documentation |
| --- | --- | --- |
| Harmony | Runtime C# method patching through a Timberborn wrapper. Declare Harmony as a required mod when using it. | [Source and setup](https://github.com/eMkaQQ/timberborn-harmony), [Harmony documentation](https://harmony.pardeike.net/) |
| Mod Settings | Shared settings UI for other mods; it adds a gear button to the mod list and has no standalone gameplay effect. The author documents Unity package and DLL-reference setup. | [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3283831040), [author's setup and API](https://github.com/eMkaQQ/timberborn-modding#mod-settings) |
| TimberUi | Helpers and prebuilt classes for recreating game-style UI elements. | [Author's description and releases](https://mod.io/g/timberborn/m/timberui), [source](https://github.com/datvm/TimberbornMods) |
| Moddable Timberborn | Opt-in helpers for specifications, templates, persistent building settings, entity tracking, effects and other systems; its documentation lists Harmony and TimberUi as dependencies. | [Documentation and demo](https://github.com/datvm/TimberbornMods/blob/master/ModdableTimberborn/Docs/README.MD) |
| TimberAPI | Additional building and tool specifications, UI builders, entity linking and other extensions. Its older releases target pre-Update 6 versions; use the release matching the target game. | [Source and features](https://github.com/Timberborn-Modding-Central/TimberAPI), [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3288241660) |

For practical community examples of code, settings and patches, see [Luke's modding guide](https://datvm.github.io/TimberbornMods/ModdingGuide/mod-settings-and-harmony). APIs and examples may lag current releases, so verify them against the installed game and library source.
