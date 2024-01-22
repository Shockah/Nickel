using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager;

public sealed class SpecializedManifestPluginLoader<TSpecializedPluginManifest, TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
	where TSpecializedPluginManifest : class, TPluginManifest
{
	private SpecializedConvertingManifestPluginLoader<TSpecializedPluginManifest, TPluginManifest, TPlugin> ConvertingPluginLoader { get; }

	public SpecializedManifestPluginLoader(IPluginLoader<TSpecializedPluginManifest, TPlugin> pluginLoader)
	{
		this.ConvertingPluginLoader = new(pluginLoader, m =>
		{
			return m is TSpecializedPluginManifest specialized
				? specialized
				: new Error<string>($"Cannot convert plugin manifest {m} to {typeof(TSpecializedPluginManifest)}.");
		});
	}

	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.ConvertingPluginLoader.CanLoadPlugin(package);

	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.ConvertingPluginLoader.LoadPlugin(package);
}
