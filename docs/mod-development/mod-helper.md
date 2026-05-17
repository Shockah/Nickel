# Mod helper

The mod helper is the main interface between a mod and Nickel. Each mod receives its own mod helper instance, which it uses to access Nickel's APIs and interact with the game.

Nickel provides the mod helper through the [`Mod`](https://github.com/Shockah/Nickel/blob/master/Nickel/Mod.cs) subclass constructor. See [the `Mod` subclass documentation page](mod-subclass.md) for more details.

## Mod registry

Accessed via `helper.ModRegistry`, this system handles inter-mod integration:
* Querying all loaded or resolved mods
* Calling other mods' exposed APIs
* Accessing other mods' mod helper, logger, and package instances

## Events

Accessed via `helper.Events`, this system allows mods to:
* Register for game lifecycle events
* Hook into or extend gameplay events
* Participate in event systems normally reserved for `Artifact` subclasses

See [Events](mod-helper/events.md) for more details.

## Content

Accessed via `helper.Content`, this system manages game content provided or modified by mods, including:
* Sprites
* Audio
* `Deck`s
* `Status`es
* `Card`s
* `Artifact`s
* Characters (playable, enemy, or event-only)
* Playable `Ship`s
* Enemies

Some of these systems may also interact with base game content depending on context.

## Mod data

Accessed via `helper.ModData`, this system is used for storing additional mod-specific data on game state objects.

See [Mod data](mod-helper/mod-data.md) for details.

## Storage

Accessed via `helper.Storage`, this system handles:
* Reading and writing files associated with the mod or save profile
* Configuring serialization behavior for mod data and game state persistence

## Utilities

Accessed via `helper.Utilities`, this system contains miscellaneous functionality that does not fit into other categories but is still useful for cross-mod or engine-level interaction.