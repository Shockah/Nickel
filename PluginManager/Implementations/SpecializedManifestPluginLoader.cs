using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager;

public sealed class SpecializedManifestPluginLoader<TSpecializedPluginManifest, TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
	where TSpecializedPluginManifest : class, TPluginManifest
{
	private SpecializedConvertingManifestPluginLoader<TSpecializedPluginManifest, TPluginManifest, TPlugin> ConvertingPluginLoader { get; }

	public SpecializedManifestPluginLoader(IPluginLoader<TSpecializedPluginManifest, TPlugin> pluginLoader)
	{
		this.ConvertingPluginLoader = new(pluginLoader, m => m as TSpecializedPluginManifest);
	}

	public bool CanLoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.ConvertingPluginLoader.CanLoadPlugin(package);

	public OneOf<TPlugin, Error<string>> LoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.ConvertingPluginLoader.LoadPlugin(package);
}
