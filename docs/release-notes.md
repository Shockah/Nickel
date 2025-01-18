[← back to readme](README.md)

# Release notes

## Upcoming release

### For everyone:
* Updated the Pintail library, allowing for a very slight performance improvement and fix for a potential bug.
* Numeric mod settings can now be Shift-clicked to adjust them faster.
* Fixed sorting modded cards by deck/rarity.

### For developers:
* Fixed the `OnSaveLoadedEvent` being available too late.

## 1.10.2
Released 14 January 2025.

* Fixed the Save Import feature crashing the game.

## 1.10.1
Released 12 January 2025.

* Fixed Mitosis-related issues. Hopefully for good.

## 1.10.0
Released 12 January 2025.

* Extended the "sort cards in order" feature to also be enabled for the discard and exhaust pile.
* Fixed card ordering on the Run Summary screen, especially for modded content.
* Reintroduced the Mitosis library, greatly improving performance.
* Updated the Pintail library, improving performance.
* Implemented caching when looking at the card codex, improving performance.
* Other performance improvements.

## 1.9.0
Released 8 January 2025.

### For everyone:
* The artifact codex now has separate categories for ship-specific and event artifacts.
* Updated the Pintail library, improving framerate.
* Other small performance improvements.

### For developers:
* The `IModCharacters(.V2)/IModShips/IModCards/IModArtifacts/IModDecks/IModStatuses/IModSprites.LookupByUniqueName` methods now return correct information for vanilla entries.

## 1.8.2
Released 2 January 2025.

* Partially reverted the special hidden artifact registration change, as it caused a different issue. The behavior should still be more in line with vanilla.

## 1.8.1
Released 31 December 2024.

### For everyone:
* Nickel is no longer registering special hidden artifacts to make some of its functionality work. This brings Nickel's behavior more in line with vanilla, making seeds compatible between them (as long as no content mods are involved).

## 1.8.0
Released 30 December 2024.

### For everyone:
* Updated the Pintail library, greatly improving mod load times.
* The "current pile" indicator icons will no longer display if currently viewing a single pile.
* The "sort cards in order" feature when browsing through cards in hand is now a proper sort mode, and it got moved from `Nickel.Bugfixes` to `Nickel.Essentials`.
* Fixed the game crashing when setting up a daily run with the Solo Run modifier with any modded characters.
* Fixed a crash when importing a save file while there are still some leftover files in the slot being imported into.

### For developers:
* Added `IModHelper.Content.Audio` which allows adding new sounds and music.
* Added the `IModEvents.OnSaveLoaded` event.
* Added `PlayableCharacterConfigurationV2.SoloStarters`.
* Added `(Non)PlayableCharacterConfigurationV2.Babble` which allows customizing the way the character talks in dialogue.
* Nickel now validates whether the provided `ExeCardType` for a playable character uses the `Deck.colorless` deck.
* Added a `Nickel.Essentials` API to control whether the "sort cards in order" sort mode is enabled for the given `CardBrowse` instance.

## 1.7.0
Released 17 December 2024.

### For everyone:
* Browsing through cards in hand for action selection will now display the cards in their order in hand.
* Fixed various bugs that could happen if card data depends on other cards.
* Split off a new internal mod `Nickel.Bugfixes` out of `Nickel.Essentials`.
* The Rock Factory status now works properly when applied to enemies.

### For developers:
* Added the `Pool` and `MultiPool` utilities.
* Added the `IModCards.OnSetCardTraitOverride` event.
* Added `DeckConfiguration.ShineColorOverride`.
* The `RequiredApiVersion` `nickel.json` field is no longer used on non-assembly mods.
* The `RequiredApiVersion` `nickel.json` field is now optional on assembly mods.
* Improved sprite asset registration and error handling.

## 1.6.1
Released 9 December 2024.

* Fixed the Logbook not always displaying the highest difficulty for a given crew combo.

## 1.6.0
Released 4 December 2024.

### For everyone:
* Updated to the latest MonoGame. Please report any issues you encounter with the game itself.
* Nickel now loads `AfterDbInit` mods as their own loading steps, making the game window actually responsive during the mod load process.
* Incorporated bugfixes from [Kokoro](https://www.nexusmods.com/cobaltcore/mods/4).
* Fixed disabled status set action rendering.

## 1.5.7
Released 18 September 2024.

### For everyone:
* Updated for the latest game version.

### For developers:
* `CurrentLocaleLocalizationProvider` and `CurrentLocaleOrEnglishLocalizationProvider` no longer crash if used when the game did not yet load any locale.

## 1.5.6
Released 30 August 2024.

* Fixed Cobalt Core 1.2 rewriters, which fixes issues with mods like Rerolls.

## 1.5.5
Released 25 August 2024.

* Rebuilt Nickel against the current version of the game (1.1.2) instead of the upcoming one (1.2). Whoops!

## 1.5.4
Released 24 August 2024.

### For players:
* Nickel no longer utilizes Mitosis. The library will be brought back after being updated and thoroughly tested.

## 1.5.3
Released 24 August 2024.

### For players:
* Updated the Mitosis library, again, fixing problems with some mods.
* To prevent further additional issues, Nickel will now fallback to the original JSON-based method if Mitosis fails.

## 1.5.2
Released 23 August 2024.

### For players:
* Updated the Mitosis library, fixing problems with some mods.

## 1.5.1
Released 23 August 2024.

### For players:
* Added more mod rewriters, improving compatibility with pre-1.2 mods on 1.2.

## 1.5.0
Released 23 August 2024.

### For players:
* Nickel is now able to load legacy mods from ZIP files (by first extracting them to a temporary folder). This is done every time Nickel starts, so if you want to avoid some extra load times, it is still recommended to extract mods manually.
* Added starting cards to character tooltips on the new run screen.
* The Logbook replacement now only activates if there are any modded characters installed.
* Fixed the Logbook replacement not showing a lot of the combo data.
* General optimizations.

### For developers:
* Nickel now rewrites mods to make mods built for pre-1.2 Cobalt Core still launch in upcoming 1.2. Some mods may still need to be updated manually.
* Plugin (mod) package paths are now case insensitive, no matter the underlying filesystem or storage method.
* `IAssemblyDefinitionEditor.EditAssemblyDefinition` now needs to return a `bool` reflecting whether the editor actually edited the assembly, and also gets a new parameter which lets an editor log messages.
* Incorporated a new [Mitosis](https://github.com/Nanoray-pl/Mitosis) library and replaced `Mutil.DeepCopy`'s implementation with one utilizing Mitosis, significantly improving copying performance.
* Updated the Pintail library, which changes how enums are proxied, improving performance and allowing flag enums to be used.

## 1.4.0
Released 1 August 2024.

### For players:
* Nickel now loads its pre-installed mods from the `InternalModLibrary` folder. These mods take priority over any duplicates found in the `ModLibrary` folder.
* The Logbook got replaced with a less pretty, but at least usable alternative.
* The tooltip scrolling feature will now only lock up other UI scrolling if the tooltip does not fit at once on screen.
* Replaced character/ship scroll buttons with properly shaded ones.
* The game will no longer crash when trying to display a run summary containing a ship or characters that are not currently loaded.
* Fixed even more card trait issues, again (fixes Summon Control artifact not working).
* Fixed hovering over ship buttons sometimes not previewing the ship.

### For developers:
* Obsoleted `DelayedHarmony` due to issues with method inlining.
* Nickel now logs passthrough prefixes separately from (potentially) skipping prefixes.

## 1.3.1
Released 25 July 2024.

### For players:
* The tooltip scrolling feature will now try not to lock up other UI scrolling, unless you hold the tooltip up for a while without interaction.

### For developers:
* Added `IsShowingShips` and `PreviewingShip` APIs to `Nickel.Essentials`.
* Added `EnglishFallbackLocalizationProvider` and `MissingPlaceholderNonBoundLocalizationProvider` implementations.
* Fixed `JsonLocalizationProvider` returning comments as strings.

## 1.3.0
Released 24 July 2024.

### For players:
* Added a new ship selector to the new run screen.
* Long tooltips are now scrollable with a mouse scroll wheel or the controller right thumbstick.
* Fixed complex card trait situations resulting in wrong states (fixes Natasha's Dark Web Data artifact).
* The game will no longer crash when trying to display a run summary containing cards or artifacts that are not currently loaded.

## 1.2.1
Released 18 July 2024.

### For players:
* Toned down on one optimization, fixing a crash when playing any card with specific mods installed together.

## 1.2.0
Released 17 July 2024.

### For players:
* Updated for Cobalt Core 1.1.2.
* General optimizations.

### For developers:
* `Artifact.ReplaceSpawnedThing` is now hookable.
* Added `ModUtilities.Harmony`, `ModUtilities.DelayedHarmony` and `ModUtilities.ApplyDelayedHarmonyPatches()`. Using `DelayedHarmony` should be preferred if possible - it improves patching performance the more mods use it.

## 1.1.0
Released 14 July 2024.

### For players:
* `Nickel.UpdateChecks` now advertises itself as just "Nickel" whenever there is an update.
* Updated the Pintail library, fixing a problem with some mods not working if Debug was disabled (the default).

### For developers:
* Added `StatusConfiguration.ShouldFlash`.
* Mods can now access the `IProxyManager` used by Nickel for proxying purposes via `IModUtilities.ProxyManager`.
* Updated the Pintail library. It now validates whether a type can be proxied before defining a proxy type, preventing an exception being thrown when `GetTypes()` was called on the assembly containing the proxy types.

## 1.0.3
Released 12 July 2024.

* Potentially fixed Nickel not being able to find the game files with some Steam Deck / Linux / Proton setups.

## 1.0.2
Released 8 July 2024.

### For players:
* [[#91](https://github.com/Shockah/Nickel/issues/91)] Split the Debug setting into two.

### For developers:
* [[#92](https://github.com/Shockah/Nickel/issues/92)] Fixed artifact hooks losing access to captured variables, seemingly accessing garbage memory.

## 1.0.1
Released 4 July 2024.

### For players:
* Fixed mod setting navigation issues on controllers.
* Fixed a mod setting visual issue on the Profile setting submenu.

## 1.0.0
Released 3 July 2024.

### For players:
* The pre-installed `Essentials` mod is now renamed to `Nickel.Essentials`. **Be sure to remove the old `Essentials` folder when updating Nickel.**
* Changed the default path where mod saves are stored from the Nickel's folder to the user's application data folder (`%AppData%\CobaltCore\Nickel\Saves`). **You will have to re-import your save files, or move the files manually.**
* Nickel now lets you resize its window.
* Improved mod load times.
* Added `Nickel.ModSettings`, creating a common interface for all mods to add their settings to.
* Added various settings for all Nickel modules.
* Update checks now remind you to set them up properly, and when your API key / token gets revoked or expires.
* Update checks now happen much earlier and even for mods which failed to load.
* The game no longer proposes memories which don't exist (for modded characters).
* The Memories menu now highlights the arrow buttons if you have any unlocked but not yet seen memories scrolled away.
* Mod descriptions now also show up for enemy characters (as long as they are added by the Nickel API).
* Nickel now ignores mods in ZIP files, if they also exist extracted.
* Nickel now sanitizes any file system paths it logs, by replacing full user home paths with just `~`.
* Nickel now has a different error log message when a mod cannot be loaded due to a missing dependency at a specific version (as compared to missing altogether).
* Fixed Nickel crashing when starting the game in a non-English language or when switching to one.
* Fixed starter deck preview sometimes showing up an extra Basic Shot on Normal difficulty, even though Hard+ was selected.
* Fixed card Codex deck filters sometimes not being properly scrollable, and weirdly jumping visually.
* Fixed controller support for the Save Import menu.

### For developers:
* Nickel now has full XML documentation for all public members.
* Added APIs for storing mod settings in a common place.
* Added APIs for copying all mod data from one object to another.
* Added APIs for registering non-playable characters (enemies or story characters).
* Added an API to register dynamic sprites.
* Added a way for mods to get a list of all resolved mods (including ones which may not have been loaded, for various reasons).
* Added additional APIs for obtaining new collision-free enum case values.
* Added an event mods can subscribe to to get informed about the game closing, either normally or via an exception being thrown.
* Added `AwaitApi` and `AwaitApiOrNull` methods, which wait until a mod is loaded before returning the API (unlike `GetApi`).
* Split `IModCards.OnGetVolatileCardTraitOverrides` into `OnGetDynamicInnateCardTraitOverrides` and `OnGetFinalDynamicCardTraitOverrides`. These can now also affect vanilla card traits.
* Actually implemented part-type-exclusive artifacts (these used to do nothing).
* `[ModBuildConfig 1.0.0]` Legacy mods can now use .NET 8 and interact with Nickel directly via a new `INickelManifest` interface.

## 0.12.0
Released 15 May 2024.

* Added update checks for installed mods.
* Fixed `Nickel.Legacy` not generating `nickel.json` files for mods after the latest changes.

### `ModBuildConfig`
`Nickel.ModBuildConfig` is also updated to 0.12.0.

* `ModBuildConfig` now validates the `Version` in the `nickel.json` file against the provided `ModVersion` property (defaulting to `Version`).

## 0.11.1
Released 13 May 2024.

* Additional improvements to the save recovering mechanism.

## 0.11.0
Released 11 May 2024.

* Replaced the current save recovering mechanism with a new one. If a save cannot be loaded, Nickel will attempt to return you to the "new run" menu, keeping other progress. If that fails, it will forcefully reset the save, while trying to keep the progress.
* Added an option to import vanilla saves into Nickel straight from the Profile Select menu.
* Added additional indicators for the current pile the card is in when browsing cards with `CardBrowse`.
* Added enemy support for mods.
* Fixed character starter artifacts showing up in various unexpected menus like the Codex Logbook.
* Improved/fixed assembly sharing between mods.
* Additional caching for card traits.

### `ModBuildConfig`
`Nickel.ModBuildConfig` is also updated to 0.11.0.

* Legacy mods' `CobaltCore.dll` is also publicized now.

## 0.10.1
Released 27 April 2024.

* Fixed card traits being half of the time active, half of the time not.

## 0.10.0
Released 27 April 2024.

* Added an API to add "volatile" card trait overrides - overrides, which act as if they were innate card traits, but which can actually depend on other traits, including other overrides.
* Fixed a crash that could occur within the new card trait system.
* Fixed EXE blacklist checkbox appearing on run summary if CAT is currently selected for a new run.
* Fixed Codex filter buttons using `Spr` directly, breaking on various game versions.
* Fixed modded ship part behavior for legacy mods, causing some of their ships to become invisible.

## 0.9.0
Released 23 April 2024.

* Essentials: Added More Difficulties alt starters indicators.
* Essentials: Added an option to disable a given EXE card from the starter pool.
* Essentials: Added a starter deck card preview.
* Essentials: Added a character starter artifact preview.
* Essentials: Fixed Isaac unlock condition.
* Added character lookup to `IModCharacters`.
* Card trait icon and tooltip callbacks' `Card` parameter is now nullable.
* Fixed card traits rendering at wrong spots, sometimes not appearing at all.
* Improved "missing string" text.

## 0.8.3
Released 18 April 2024.

* Playable characters and ships are now sorted by their mod's `UniqueName`.
* Fixed a crash when serializing some API proxied types, again.
* Fixed a crash when multiple mods registered for the same artifact hook with different arguments.

## 0.8.2
Released 15 April 2024.

* Fixed temporary card traits being so temporary, that they disappear before the cards get to be removed from the deck.

## 0.8.1
Released 14 April 2024.

* Updated the `Shrike.Harmony` library to 3.1.1, fixing an error in `Essentials`.

## 0.8.0
Released 14 April 2024.

* Updated the `Shrike` and `Shrike.Harmony` libraries to 3.1.0.
* Added support for custom card traits.
* Added `GlossaryTooltip` to be used by mods, which mimicks vanilla `TTGlossary`, but accepts any content.
* Fixed a crash when serializing some API proxied types.
* Fixed re-registered cards counting twice for codex, preventing full completion.
* Improved `Nickel.ModBuildConfig` NuGet package, which should hopefully fix any issues with extracting the `CobaltCore.dll` file from the `CobaltCore.exe` file provided by the game.

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