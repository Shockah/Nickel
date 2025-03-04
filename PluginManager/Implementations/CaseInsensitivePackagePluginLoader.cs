using Nanoray.PluginManager.CaseInsensitive;
using OneOf;
using OneOf.Types;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginLoader{TPluginManifest,TPlugin}"/> which makes all plugin packages case insensitive.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPlugin">The plugin type.</typeparam>
public sealed class CaseInsensitivePluginLoader<TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
{
	private readonly IPluginLoader<TPluginManifest, TPlugin> Loader;
	private readonly Dictionary<IPluginPackage<TPluginManifest>, IPluginPackage<TPluginManifest>> WrappedPackages = [];

	/// <summary>
	/// Creates a new <see cref="CaseInsensitivePluginLoader{TPluginManifest,TPlugin}"/>.
	/// </summary>
	/// <param name="loader">An underlying plugin loader.</param>
	public CaseInsensitivePluginLoader(IPluginLoader<TPluginManifest, TPlugin> loader)
	{
		this.Loader = loader;
	}

	/// <inheritdoc/>
	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.Loader.CanLoadPlugin(this.ObtainWrappedPackage(package));

	/// <inheritdoc/>
	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.Loader.LoadPlugin(this.ObtainWrappedPackage(package));
	
	private IPluginPackage<TPluginManifest> ObtainWrappedPackage(IPluginPackage<TPluginManifest> package)
	{
		ref var wrappedPackage = ref CollectionsMarshal.GetValueRefOrAddDefault(this.WrappedPackages, package, out var wrappedPackageExists);
		if (!wrappedPackageExists)
			wrappedPackage = new DirectoryPluginPackage<TPluginManifest>(package.Manifest, new CaseInsensitiveDirectoryInfo(package.PackageRoot));
		return wrappedPackage!;
	}
}
