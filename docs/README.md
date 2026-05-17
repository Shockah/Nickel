# Nickel

**Nickel** is a modding API / mod loader for the game [Cobalt Core](https://store.steampowered.com/app/2179850/Cobalt_Core/) by [Rocket Rat Games](https://rocketrat.games/). It is completely independent from the main game and does not modify any of its files.

Nickel aims to provide a stable, user-friendly modding experience for both players and mod developers.

The main purposes of Nickel are:

1. **Be completely independent from the game.** Nickel does not modify any game files or use the same save files. Users can still launch the vanilla game with their existing progress at any time.
2. **Resolve and load mods.** Nickel scans a folder for mods, automatically determines their load order based on their dependencies, and loads the mods in that order. Depending on a mod's needs, it can load in one of the predefined phases -- before the game's code gets loaded, after it is loaded, or after the game initializes its database.
3. **Set up common utilities for mods.** These utilities include: [Harmony](https://github.com/pardeike/Harmony), [Mono.Cecil](https://github.com/jbevain/cecil), [Shrike](https://github.com/Nanoray-pl/Shrike).
4. **Allow editing the game's code before it is loaded.** Combining the [Mono.Cecil](https://github.com/jbevain/cecil) library and being able to load before the game's assembly lets mods do some normally impossible things by directly editing the game's code before it even gets a chance to get loaded.
5. **Capture logs useful for debugging issues.** Nickel captures logs from itself, installed mods, and the game into a single log file. This information can help diagnose issues specific to modded setups.
6. **Automatically fix save files.** Nickel will try to recover otherwise unusable save files. This can often happen when adding, removing or updating mods.

## Downloads

Get the latest version from:
* [NexusMods](https://www.nexusmods.com/cobaltcore/mods/1)
* [GitHub releases](https://github.com/Shockah/Nickel/releases/latest)

## Documentation

### General

* [Release notes](release-notes/nickel.md)

### For players

* [Player guide](player-guide.md)

### For developers

* [`nickel.json`](mod-development/nickel-json.md)
* [Serialization](mod-development/serialization.md)
* [Mod helper events](mod-development/mod-helper/events.md)
* [Mod data](mod-development/mod-helper/mod-data.md)
* [`ModBuildConfig` release notes](release-notes/mod-build-config.md)