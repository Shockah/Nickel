using OneOf;
using OneOf.Types;
using System;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginLoader{TPluginManifest,TPlugin}"/> which loads plugins conditionally.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPlugin">The plugin type.</typeparam>
public sealed class ConditionalPluginLoader<TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
{
	private IPluginLoader<TPluginManifest, TPlugin> Loader { get; }
	private Func<IPluginPackage<TPluginManifest>, OneOf<Yes, No, Error<string>>> Condition { get; }

	/// <summary>
	/// Creates a new <see cref="ConditionalPluginLoader{TPluginManifest,TPlugin}"/>.
	/// </summary>
	/// <param name="loader">An underlying plugin loader.</param>
	/// <param name="condition">A function checking whether a plugin should be loaded at all.</param>
	public ConditionalPluginLoader(IPluginLoader<TPluginManifest, TPlugin> loader, Func<IPluginPackage<TPluginManifest>, OneOf<Yes, No, Error<string>>> condition)
	{
		this.Loader = loader;
		this.Condition = condition;
	}

	/// <inheritdoc/>
	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		var yesNoOrError = this.Condition(package);
		if (!yesNoOrError.IsT0)
			return yesNoOrError;
		return this.Loader.CanLoadPlugin(package);
	}

	/// <inheritdoc/>
	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.Loader.LoadPlugin(package);
}
