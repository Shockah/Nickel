# Nickel

**Nickel** is a modding API / mod loader for the game [Cobalt Core](https://store.steampowered.com/app/2179850/Cobalt_Core/) by [Rocket Rat Games](https://rocketrat.games/). It is completely independent from the main game and does not modify any of its files.

The main purposes of Nickel are:

1. **Be completely independent from the game.** Nickel does not modify any game files or use the same save files -- if the user wants to, they can still launch the vanilla game with the progress they already had.
2. **Resolve and load mods.** Nickel finds all mods in a given folder, automatically determines their load order based on their dependencies, and finally loads the mods in that order. Depending on the mods' needs, they can load in one of the predefined phases -- before the game's code gets loaded, after it is loaded, or after the game initializes its database.
3. **Set up common utilities for mods.** These utilities include but are not limited to: [Harmony](https://github.com/pardeike/Harmony), [Mono.Cecil](https://github.com/jbevain/cecil), [Shrike](https://github.com/Nanoray-pl/Shrike).
4. **Expose the ability to edit the game's code before it gets loaded.** Combining the [Mono.Cecil](https://github.com/jbevain/cecil) library and being able to load before the game's assembly lets mods do some normally impossible things by directly editing the game's code before it even gets a chance to get loaded.
5. **Capture logs useful for debugging issues.** Nickel will capture all kinds of logs in one common file. This includes any actions Nickel does, anything mods log by themselves, and anything the game logs. All of this information together can help figuring out issues specific to the modded game.
6. **Automatically fix save files.** Nickel will try to recover a save file from being completely unusable. This can often happen when adding, removing or updating mods.

## Documentation

### For players

* [Player guide](https://github.com/Shockah/Nickel/blob/master/docs/player-guide.md)

### Other

* [Release notes](https://github.com/Shockah/Nickel/blob/master/docs/release-notes.md)