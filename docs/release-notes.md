[← back to readme](README.md)

# Release notes

## 0.7.0
Released 10 February 2024.

* Added `Nickel::OnAfterGameAssemblyPhaseFinished` and `Nickel::OnAfterDbInitPhaseFinished` events for legacy mods' event hubs.
* The Crystallized Friend event now grants the selected character's starter artifacts.
* Fixed Steam achievements not working.
* Fixed registering artifact hooks in the `AfterGameAssembly` phase throwing exceptions.
* Fixed legacy mods using `EventHub` each having their own hub, instead of a shared one.
* Deprecated `CharacterConfiguration.StarterCardTypes` and `CharacterConfiguration.StarterArtifactTypes` in favor of `CharacterConfiguration.Starters`.

## 0.6.1
Released 8 February 2024.

* Added a `--vanilla` commandline flag, mostly for Thunderstore integration.

## 0.6.0
Released 3 February 2024.

* Nickel is now high-DPI aware - it should scale the same way vanilla does. Keep in mind this can make the debug menu very tiny on high resolution screens. If needed, this can be addressed in a future update.
* Added sprite lookup to `IModSprites`.
* Essentials: General card codex filtering improvements (including performance).
* Fixed some `IFileInfo`/`IDirectoryInfo` path relativity issues.
* Fixed non-`void` artifact hooks crashing when registered.
* General system stability improvements to enhance the user's experience :tm:.

## 0.5.0
Released 27 January 2024.

* Added support for registering EXE cards for new playable characters.
* Improved mod error handling - Nickel should no longer completely fail when a mod fails to *load* - the mod will be skipped instead.
* Nickel and Nickel.Legacy now validate assembly versions against data provided in `nickel.json` files and will log a warning on mismatch.

## 0.4.3
Released 22 January 2024.

* Added an experimental legacy mod change, calling `BootMod` during `Mod` instance construction. This should allow using mod APIs properly when the dependencies are set correctly, but has the potential to break if mods didn't use `GetApi` right.
* Fixed artifact hooks possibly being called on the same mod multiple times, instead of each mod once.
* Fixed `ModData` deserialization of non-primitive types.

## 0.4.2
Released 20 January 2024.

* The Essentials mod now adds deck filtering in the card codex menu.
* Updated the Pintail library to 2.4.2, which fixes even more issues with proxying default interface method implementations.

## 0.4.1
Released 20 January 2024.

* Updated the Pintail library to 2.4.1, which fixes issues with proxying default interface method implementations.
* Fixed console logs not being colored on some terminals.

## 0.4.0
Released 18 January 2024.

* Nickel now has an icon!
* Initializing Steam even in debug mode.
* Essentials: Controller support for crew selection.
* Essentials: Memories screen is now scrollable, and also supports controllers.
* Cards and artifacts no longer register themselves as their `Type.Name`, so they can no longer conflict. This however means that codexes will have to be filled in again.
* Added a `ModData` system (akin to Kokoro's `ExtensionData`).
* Added a LOT more logs.
* Various bugfixes.

### `ModBuildConfig`
`Nickel.ModBuildConfig` is also updated to 0.4.0.

## 0.3.0
Released 14 January 2024.

* Added auto-generation of `nickel.json` files for legacy mods.
* Added automatic creation of "Character is missing" statuses.
* Added some basic save file fallbacks when removing modded content.
* Changed the preferred `ModType` to `Nickel` instead of `Nickel.Assembly` - Nickel will still load both for now though.
* Some small public API changes and new overloads.
* Various bugfixes.

## 0.2.0
Released 10 January 2024.

* Essentials: Added mod source tooltips -- enable these by holding Alt.
* Added a way to lookup modded content by their unique name.
* Publicizing the `CobaltCore.dll` assembly.
* Added missing reference to the `OneOf` library in `ModBuildConfig`.
* Fixed not reusing assemblies provided by the game.
* Fixed the `AfterDbInit` phase actually being too late.
* Fixed character serialization.
* Fixed hookable artifacts showing up on run summary screen.

## 0.1.0
Released 9 January 2024.

* Initial alpha release.