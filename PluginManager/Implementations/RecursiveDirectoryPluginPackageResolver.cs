using OneOf;
using OneOf.Types;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

public sealed class RecursiveDirectoryPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IDirectoryInfo Directory { get; }
	private string ManifestFileName { get; }
	private bool IgnoreDotDirectories { get; }
	private IPluginManifestLoader<TPluginManifest> PluginManifestLoader { get; }

	public RecursiveDirectoryPluginPackageResolver(IDirectoryInfo directory, string manifestFileName, bool ignoreDotDirectories, IPluginManifestLoader<TPluginManifest> pluginManifestLoader)
	{
		this.Directory = directory;
		this.ManifestFileName = manifestFileName;
		this.IgnoreDotDirectories = ignoreDotDirectories;
		this.PluginManifestLoader = pluginManifestLoader;
	}

	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
		=> this.ResolveChildDirectories(this.Directory);

	private IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePackagesAndThenResolveChildDirectories(IDirectoryInfo directory)
	{
		foreach (var package in new DirectoryPluginPackageResolver<TPluginManifest>(directory, this.ManifestFileName, this.PluginManifestLoader).ResolvePluginPackages())
			yield return package;

		// stop recursing if there is a manifest file
		if (directory.GetChild(this.ManifestFileName).Exists)
			yield break;

		foreach (var package in this.ResolveChildDirectories(directory))
			yield return package;
	}

	private IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolveChildDirectories(IDirectoryInfo directory)
	{
		foreach (var childDirectory in directory.Directories)
			if (!this.IgnoreDotDirectories || !directory.Name.StartsWith("."))
				foreach (var package in this.ResolvePackagesAndThenResolveChildDirectories(childDirectory))
					yield return package;
	}
}
