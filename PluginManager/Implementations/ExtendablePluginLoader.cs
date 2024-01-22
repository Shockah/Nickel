using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

public sealed class ExtendablePluginLoader<TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
{
	private List<IPluginLoader<TPluginManifest, TPlugin>> Loaders { get; } = [];

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

	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		foreach (var loader in this.Loaders)
			if (loader.CanLoadPlugin(package).IsT0)
				return loader.LoadPlugin(package);
		throw new ArgumentException($"This plugin loader cannot load the plugin package {package}.");
	}

	public void RegisterPluginLoader(IPluginLoader<TPluginManifest, TPlugin> loader)
		=> this.Loaders.Add(loader);

	public void UnregisterPluginLoader(IPluginLoader<TPluginManifest, TPlugin> loader)
		=> this.Loaders.Remove(loader);
}
