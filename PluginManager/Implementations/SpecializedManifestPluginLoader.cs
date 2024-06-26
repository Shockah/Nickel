using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginLoader{TPluginManifest,TPlugin}"/> which only loads plugins for a specialized plugin manifest subclass.
/// </summary>
/// <typeparam name="TSpecializedPluginManifest">The specialized type of the plugin manifest.</typeparam>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPlugin">The plugin type.</typeparam>
public sealed class SpecializedManifestPluginLoader<TSpecializedPluginManifest, TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
	where TSpecializedPluginManifest : class, TPluginManifest
{
	private SpecializedConvertingManifestPluginLoader<TSpecializedPluginManifest, TPluginManifest, TPlugin> ConvertingPluginLoader { get; }

	/// <summary>
	/// Creates a new <see cref="SpecializedManifestPluginLoader{TSpecializedPluginManifest,TPluginManifest,TPlugin}"/>.
	/// </summary>
	/// <param name="pluginLoader">The underlying plugin loader.</param>
	public SpecializedManifestPluginLoader(IPluginLoader<TSpecializedPluginManifest, TPlugin> pluginLoader)
	{
		this.ConvertingPluginLoader = new(
			pluginLoader,
			m => m is TSpecializedPluginManifest specialized ? specialized : new Error<string>($"Cannot convert plugin manifest {m} to {typeof(TSpecializedPluginManifest)}.")
		);
	}

	/// <inheritdoc/>
	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.ConvertingPluginLoader.CanLoadPlugin(package);

	/// <inheritdoc/>
	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.ConvertingPluginLoader.LoadPlugin(package);
}
