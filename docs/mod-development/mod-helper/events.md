# Mod helper events

Nickel provides several events that mods can subscribe to.

These are accessed via your mod's [mod helper instance [TODO]](TODO):
```
helper.Events.[...]
```

## `OnModLoadPhaseFinished`

An event fired whenever a mod load phase finishes. Passes in the phase that just finished loading.

See [the `nickel.json` documentation](../nickel-json.md#modloadphase) for more details on mod load phases.

> [!TIP]
> Combined with checking whether the phase is `AfterDbInit`, this event can be used to run code after ensuring all mods are loaded.

```cs
helper.Events.OnModLoadPhaseFinished += (_, phase) =>
{
	if (phase != ModLoadPhase.AfterDbInit)
		return;

	// all mods are loaded by now; run your code here
}
```

## `OnModLoaded`

An event fired whenever a mod is loaded. Passes in information about the mod that was loaded.

```cs
helper.Events.OnModLoaded += (_, package) =>
{
	if (package.Manifest.UniqueName != "AuthorName.VerySpecialModICareAbout")
		return;

	// the specific mod just got loaded; interact with it here
}
```

## `OnSaveLoaded`

An event fired whenever the active save state changes or finishes loading. Passes in the game `State` of the newly loaded data.

```cs
helper.Events.OnSaveLoaded += (_, state) =>
{
	// handle the save file - for example, change some game database state depending on the current run
}
```

## `OnLoadStringsForLocale`

An event fired whenever the game loads a localization (when the game starts, and whenever the selected language changes). Passes in the locale code, as well as the current string localization dictionary.

```cs
helper.Events.OnLoadStringsForLocale += (_, e) =>
{
	if (e.Locale != "en")
		return;

	e.Localizations["KeyToOverride"] = "New localized text for the key";
}
```

## `OnGameClosing`

An event fired when the game is about to close, either normally or via an exception being thrown. Passes in the exception causing the shutdown, if any.

> [!WARNING]
> This event will not be fired for critical exceptions, like [`StackOverflowException`](https://learn.microsoft.com/en-us/dotnet/api/system.stackoverflowexception) or [`AccessViolationException`](https://learn.microsoft.com/en-us/dotnet/api/system.accessviolationexception).

```cs
helper.Events.OnGameClosing += (_, ex) =>
{
	// do game exit cleanup, if any is needed
}
```

## Artifact hook registration

The events helper also contains methods related to [Artifact hooks](events/artifact-hooks.md).