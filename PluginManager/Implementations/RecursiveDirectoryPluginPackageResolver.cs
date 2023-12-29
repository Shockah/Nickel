using System.Collections.Generic;
using System.IO;
using OneOf.Types;
using OneOf;

namespace Nanoray.PluginManager;

public sealed class RecursiveDirectoryPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private DirectoryInfo Directory { get; }
	private string ManifestFileName { get; }
	private bool IgnoreDotDirectories { get; }
	private IPluginManifestLoader<TPluginManifest> PluginManifestLoader { get; }

	public RecursiveDirectoryPluginPackageResolver(DirectoryInfo directory, string manifestFileName, bool ignoreDotDirectories, IPluginManifestLoader<TPluginManifest> pluginManifestLoader)
	{
		this.Directory = directory;
		this.ManifestFileName = manifestFileName;
		this.IgnoreDotDirectories = ignoreDotDirectories;
		this.PluginManifestLoader = pluginManifestLoader;
	}

	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
		=> ResolveChildDirectories(this.Directory);

	private IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePackagesAndThenResolveChildDirectories(DirectoryInfo directory)
	{
		foreach (var package in new DirectoryPluginPackageResolver<TPluginManifest>(directory, this.ManifestFileName, this.PluginManifestLoader).ResolvePluginPackages())
			yield return package;

		// stop recursing if there is a manifest file
		FileInfo manifestFile = new(Path.Combine(directory.FullName, this.ManifestFileName));
		if (manifestFile.Exists)
			yield break;

		foreach (var package in this.ResolveChildDirectories(directory))
			yield return package;
	}

	private IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolveChildDirectories(DirectoryInfo directory)
	{
		foreach (var childDirectory in directory.GetDirectories())
			if (!this.IgnoreDotDirectories || !directory.Name.StartsWith("."))
				foreach (var package in this.ResolvePackagesAndThenResolveChildDirectories(childDirectory))
					yield return package;
	}
}
