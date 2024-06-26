namespace Nanoray.PluginManager;

/// <summary>
/// Describes the behavior that should happen if a <see cref="IPluginPackageResolver{TPluginManifest}"/> is meant to only return a single result, but finds no manifest.
/// </summary>
public enum SingleFilePluginPackageResolverNoManifestResult
{
	/// <summary>The <see cref="IPluginPackageResolver{TPluginManifest}"/> should yield no results.</summary>
	Empty,
	
	/// <summary>The <see cref="IPluginPackageResolver{TPluginManifest}"/> should yield an error.</summary>
	Error
}
