using OneOf;
using OneOf.Types;
using System;

namespace Nanoray.PluginManager;

public sealed class ConditionalPluginLoader<TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
{
	private IPluginLoader<TPluginManifest, TPlugin> Loader { get; }
	private Func<IPluginPackage<TPluginManifest>, OneOf<Yes, No, Error<string>>> Condition { get; }

	public ConditionalPluginLoader(IPluginLoader<TPluginManifest, TPlugin> loader, Func<IPluginPackage<TPluginManifest>, OneOf<Yes, No, Error<string>>> condition)
	{
		this.Loader = loader;
		this.Condition = condition;
	}

	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		var yesNoOrError = this.Condition(package);
		if (!yesNoOrError.IsT0)
			return yesNoOrError;
		return this.Loader.CanLoadPlugin(package);
	}

	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.Loader.LoadPlugin(package);
}
