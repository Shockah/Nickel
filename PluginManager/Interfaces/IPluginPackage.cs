using System;

namespace Nanoray.PluginManager;

public interface IPluginPackage<out TPluginManifest> : IDisposable
{
	TPluginManifest Manifest { get; }
	IDirectoryInfo PackageRoot { get; }
}
