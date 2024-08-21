using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginLoader{TPluginManifest,TPlugin}"/> which allows registering additional <see cref="IPluginLoader{TPluginManifest,TPlugin}"/> implementations.
/// Each implementation is invoked sequentially, and errors are only returned if all of them return an error.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPlugin">The plugin type.</typeparam>
public sealed class ExtendablePluginLoader<TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
{
	private readonly List<IPluginLoader<TPluginManifest, TPlugin>> Loaders = [];

	/// <inheritdoc/>
	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		foreach (var loader in this.Loaders)
		{
			var yesNoOrError = loader.CanLoadPlugin(package);
			if (yesNoOrError.IsT1)
				continue;
			return yesNoOrError;
		}
		return new No();
	}

	/// <inheritdoc/>
	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		foreach (var loader in this.Loaders)
			if (loader.CanLoadPlugin(package).IsT0)
				return loader.LoadPlugin(package);
		throw new ArgumentException($"This plugin loader cannot load the plugin package {package}.");
	}

	/// <summary>
	/// Register a plugin loader.
	/// </summary>
	/// <param name="loader">The plugin loader.</param>
	public void RegisterPluginLoader(IPluginLoader<TPluginManifest, TPlugin> loader)
		=> this.Loaders.Add(loader);

	/// <summary>
	/// Unregister a plugin loader.
	/// </summary>
	/// <param name="loader">The plugin loader.</param>
	public void UnregisterPluginLoader(IPluginLoader<TPluginManifest, TPlugin> loader)
		=> this.Loaders.Remove(loader);
}
