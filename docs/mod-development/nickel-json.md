# `nickel.json`

The `nickel.json` file is placed in the root directory of a mod and describes the mod and everything Nickel needs to know about it before it is loaded. Every single mod needs this file to be recognized by Nickel.

Example file:

```json
{
	"UniqueName": "Nickel.UpdateChecks.UI",
	"Version": "{{ModVersion}}",
	"DisplayName": "Nickel: Update checks UI",
	"Description": "Adds actual UI and a way to access settings for mod update checks.",
	"Author": "Nickel",
	"MinimumGameVersion": "1.2.5",
	"RequiredApiVersion": "{{ModVersion}}",
	"EntryPointAssembly": "Nickel.UpdateChecks.UI.dll",
	"EntryPointType": "Nickel.UpdateChecks.UI.ModEntry",
	"Dependencies": [
		{
			"UniqueName": "Nickel.UpdateChecks",
			"Version": "{{ModVersion}}"
		},
		{
			"UniqueName": "Nickel.ModSettings",
			"Version": "{{ModVersion}}"
		},
		{
			"UniqueName": "Nickel.InfoScreens",
			"Version": "{{ModVersion}}",
			"IsRequired": false
		}
	]
}
```

# Fields for all mods

## `UniqueName`

The `UniqueName` field represents the unique identifier for the mod, usually in the format of `{AuthorName}.{ModName}`. This field is **required**.

Mods can reference each other with this identifier - both in their manifests, as well as at runtime.

> [!WARNING]
> References to content from a mod will include its identifier. This means that once this field is set and used in the wild, it should never be changed, unless you want to risk corrupting save data.

## `Version`

The mod's version. This field is optional, but recommended to be provided. Defaults to `"1.0.0"`.

This version is presented to the players, used in update checks, and also used in mod dependency resolving.

> [!TIP]
> When using the [`Nickel.ModBuildConfig`](https://www.nuget.org/packages/Nickel.ModBuildConfig) NuGet package, your input `nickel.json` file may contain a `{{ModVersion}}` tag, which will be automatically replaced with the `Version` property from your mod's `.csproj` project in the built mod.

## `DisplayName`

A user-friendly name of the mod, presented instead of the `UniqueName`. This field is optional.

## `Description`

A user-friendly description of the mod. This field is optional.

## `Author`

A user-friendly string listing the authors of the mod. This field is optional.

## `ModType`

Describes what kind of mod this is. This field is optional. Defaults to `"Nickel"`.

`Nickel` mods are .NET mods, usually written in C#, loaded by Nickel by itself.

Nickel allows mods to register themselves as handling different mod types - for example:
* A `Nickel` mod could enable loading Python- or Lua-based mods.
* A `Nickel` mod could make editing bits of content possible with just `.json` files, and also be distributed as proper mods.

## `ModLoadPhase`

Defines in which load phase this mod will be loaded. This field is optional. Defaults to `"AfterGameAssembly"`.

### `BeforeGameAssembly`

This phase happens very early into the startup process, after the game path is resolved, but before the game assembly is loaded. None of the game's types or content are available yet. Useful for mods which should be loaded regardless (for example, for checking the updates to all resolved mods), or for mods which need to use advanced techniques, like editing the game or other mods' assemblies before they get loaded.

### `AfterGameAssembly`

This phase happens right after the game assembly is loaded, but the game hasn't started loading its data yet. All of the game's code is available at this point and can be used in the mod, but the game's databases and initialized content may not yet be ready to use.

### `AfterDbInit`

This phase happens at the very end of the game's normal loading process. Mods loading in this phase are actually visually represented as part of the loading bar. All of the game's code is available at this point, as well as the game's databases and initialized content being fully ready to use.

## `MinimumGameVersion`

The minimum version of the game required to correctly load the mod. This field is optional.

> [!NOTE]
> This field cannot be used with mods loaded during the `BeforeGameAssembly` mod load phase, since the version of the game is not known at that time.

## `UnsupportedGameVersion`

The first game version that is incompatible with this mod. The mod will not load on this version or any newer versions. This field is optional.

## `Dependencies`

A list of mods that should be loaded before this mod, optionally with version requirements.

Each entry consists of several fields.

### `UniqueName`

The unique identifier for the mod that should load before this mod. This field is **required**.

### `IsRequired`

Whether the dependency is required. If it is, this mod will not be loaded if the dependency is not present. This field is optional. Defaults to `true`.

### `Version`

The minimum version of the dependency that needs to be present. This field is optional.

> [!NOTE]
> This field also applies to optional dependencies. If a version for an optional dependency is specified, this mod will only load either if the dependency is not present, or if it's present *and* at the correct version.

## `UpdateChecks`

Specifies additional information that will let Nickel check for updates for this mod. This field is optional.

Nickel can check multiple update providers for the same mod.

This capability is provided by the built-in `Nickel.UpdateChecks` mod. If disabled, this field will be ignored.

This entry consists of several fields.

### `NexusMods`

If the mod is available for download on [NexusMods](https://www.nexusmods.com/games/cobaltcore/mods), this field can be used to allow automatic update checks for it. This field is optional.

Nickel considers the mod version visible on the main page of the mod on NexusMods as the latest.

This capability is provided by the built-in `Nickel.UpdateChecks.NexusMods` mod. If disabled, this field will be ignored.

This entry consists of one field.

#### `ID`

The NexusMods page ID of the mod. This field is **required**.

This ID can be retrieved either after publishing a mod page, or after the first step of mod page creation. The ID is present in the URL.

### `GitHub`

If the mod is available for download on [GitHub](https://github.com/), this field can be used to allow automatic update checks for it. This field is optional.

Nickel considers the latest release matching the configured release filters on GitHub as the latest.

This capability is provided by the built-in `Nickel.UpdateChecks.GitHub` mod. If disabled, this field will be ignored.

This entry consists of several fields.

#### `Repository`

The full name of the repository hosting releases for the mod - for example: `Shockah/Nickel`. This field is **required**.

#### `ReleaseTagRegex`

A [regex](https://en.wikipedia.org/wiki/Regular_expression) matching release tags for the mod. This field is optional.

This field may be useful if your repository hosts multiple mods, and you need to disambiguate releases between them.

#### `ReleaseNameRegex`

A [regex](https://en.wikipedia.org/wiki/Regular_expression) matching release names for the mod. This field is optional.

This field may be useful if your repository hosts multiple mods, and you need to disambiguate releases between them.

> [!NOTE]
> If `ReleaseTagRegex` is specified, Nickel first attempts to match it against the release tag. If that does not match, `ReleaseNameRegex` is checked instead.

### `ModNameForUpdatePurposes`

A custom display name used by update check UIs instead of the mod's normal `DisplayName`. For example, this is used internally by the `Nickel.UpdateChecks` mod to appear as just "Nickel", and provide update checks for it. This field is optional.

### Examples

```json
{
	"UpdateChecks": {
		"ModNameForUpdatePurposes": "Nickel",
		"NexusMods": {
			"ID": 1
		},
		"GitHub": {
			"Repository": "Shockah/Nickel"
		}
	}
}
```

```json
{
	"UpdateChecks": {
		"GitHub": {
			"Repository": "rft50/cc-dave",
			"ReleaseTagRegex": "^jester\\-(.*)"
		}
	}
}
```

# Fields for `Nickel` type mods

See the `ModType` field for more details on mod types.

## `EntryPointAssembly`

The name of the `.dll` file containing the entry point for the mod, usually `{ProjectName}.dll`. This field is **required**.

## `EntryPointType`

The namespace-qualified name of the type that is the entry point for the mod (a subclass of [`Mod`](https://github.com/Shockah/Nickel/blob/master/Nickel/Mod.cs)), contained in the `.dll` file pointed to by the `EntryPointAssembly` field. This field is optional.

If this field is not provided, Nickel will automatically attempt to find the entry point. This can fail if there are multiple candidates.

## `RequiredApiVersion`

The minimum version of Nickel required to correctly load this mod. This field is optional.

## `AssemblyReferences`

A list of additional assemblies this mod uses. This field is optional.

By default, all additional assemblies are loaded automatically without needing to be explicitly listed. This field lets you override how they are loaded.

> [!NOTE]
> If an assembly is not explicitly listed in `AssemblyReferences`, it behaves as if `"IsShared": false` was specified for it.

Each entry consists of several fields.

### `Name`

The assembly name (not the file name). This field is **required**.

### `IsShared`

Whether the assembly should be shared between all mods (if `true`), or loaded into the mod's private context (if `false`). This field is optional. Defaults to `true`.
* A shared assembly can only be loaded once, but can be easily referenced between mods.
* A private assembly can be loaded for each mod separately, even at different versions, but their objects can't be easily shared.

### Example

```json
{
    // ...
    "AssemblyReferences": [
        {
            "Name": "System.Memory.Data",
            "IsShared": true
        }
    ]
}
```

## `MethodsToStopInlining`

A list of .NET methods that Nickel should prevent from being inlined when this mod is installed. This field is optional.

This is an advanced feature primarily intended for compatibility with runtime patching techniques. See the [code patching [TODO]](TODO) page for more details.

Each entry consists of several fields.

### `AssemblyName`

A [regex](https://en.wikipedia.org/wiki/Regular_expression) matching the name of the assembly containing the method to stop from inlining. This field is optional. Defaults to the game's assembly name.

### `TypeName`

A [regex](https://en.wikipedia.org/wiki/Regular_expression) matching the name of the type containing the method to stop from inlining. This field is **required**.

### `MethodName`

A [regex](https://en.wikipedia.org/wiki/Regular_expression) matching the name of the method to stop from inlining. This field is **required**.

### `ArgumentCount`

The number of arguments the method to stop from inlining has. This field is optional.

### `IgnoreNoMatches`

Whether Nickel should ignore if this entry didn't match any methods. If not ignored, a warning will be produced. This field is optional. Defaults to `false`.

### Example

```json
{
    // ...
    "MethodsToStopInlining": [
        {
            "TypeName": "Combat",
            "MethodName": "IsVisible"
        }
    ]
}
```

# Semantic Versioning

All version fields in `nickel.json` files follow [Semantic Versioning](https://semver.org/).

> [!NOTE]
> Versions with labels - such as `-alpha` - are considered "lower" versions than the same versions without such labels. For example, `1.4.0-alpha1` is lower than both `1.4.0` and `1.5.0`, but higher than `1.3.2`.