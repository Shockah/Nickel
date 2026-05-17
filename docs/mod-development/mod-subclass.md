# `Mod` subclass

A [`Mod`](https://github.com/Shockah/Nickel/blob/master/Nickel/Mod.cs) subclass is the entry point for each `Nickel`-type mod (see [the `nickel.json` documentation page](nickel-json.md) for more information on mod types).

During the mod load process, Nickel loads the mod's entry point assembly `.dll` file and creates an instance of the mod's `Mod` subclass.

The entry point can either be explicitly configured, or automatically detected when the assembly contains exactly one `Mod` subclass.

## `SimpleMod`

Nickel comes with a [`SimpleMod`](https://github.com/Shockah/Nickel/blob/master/Nickel/Mod.cs) class - a convenience `Mod` subclass with predefined constructor parameters and properties for accessing the mod package, mod helper, and logger.

## Constructor

Nickel instantiates the `Mod` subclass through its constructor and can automatically provide certain parameter types via limited dependency injection.

### Common `Mod` constructor parameters

#### `IPluginPackage<IModManifest>`

Provides access to the mod's package, including:
* The mod manifest (contents of the `nickel.json` file).
* All files bundled with the mod.

#### `IModManifest`

Provides access to the mod's manifest (contents of the `nickel.json` file).

#### `IModHelper`

Provides the mod's own [mod helper](mod-helper.md) instance, which can be used to interact with Nickel's APIs.

#### `ILogger`

Provides a mod-specific logger which writes messages to both the console and Nickel's log file.

### Advanced mod integration `Mod` constructor parameters

#### `ExtendablePluginLoader<IModManifest, Mod>`

Lets a mod register additional plugin loaders, enabling support for new mod types (see [the `nickel.json` documentation page](nickel-json.md) for more information on mod types).

#### `ExtendableAssemblyDefinitionEditor`

Lets a mod edit .NET assemblies (either the game's or another mod's) before they are loaded via [Mono.Cecil](https://github.com/jbevain/cecil).

#### `IAssemblyPluginLoaderLoadContextProvider<IAssemblyModManifest>`

Lets a mod query Nickel for the [`AssemblyLoadContext`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext) associated with a given plugin package.

This can be useful for advanced assembly loading scenarios.

#### `ExtendableAssemblyPluginLoaderParameterInjector<IModManifest>`

Lets a mod register additional parameter types that can be injected during the dependency injection process.

# Example mod

```cs
using Nickel;

namespace MyName.ExampleMod;

public sealed class ModEntry : SimpleMod
{
	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		// you can use `package`, `helper` and `logger` here
	}

	private void SomeMethodCalledElsewhere()
	{
		// you can use `this.Package`, `this.Helper` and `this.Logger` here
	}
}
```