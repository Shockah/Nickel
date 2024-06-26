using System;

namespace Nanoray.PluginManager;

/// <summary>
/// Describes a plugin package.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public interface IPluginPackage<out TPluginManifest> : IDisposable
{
	/// <summary>The plugin's manifest.</summary>
	TPluginManifest Manifest { get; }
	
	/// <summary>The root directory of the plugin package.</summary>
	IDirectoryInfo PackageRoot { get; }
}
