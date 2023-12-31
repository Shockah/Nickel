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
		=> this.ResolvePluginPackages(this.Directory, isRoot: true);

	private IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages(IDirectoryInfo directory, bool isRoot = false)
	{
		var manifestFile = directory.GetRelativeFile(this.ManifestFileName);
		if (manifestFile.Exists)
		{
			if (isRoot)
			{
				yield return new Error<string>($"Found a manifest file at `{manifestFile.FullName}`, but it's in the root of the mods folder.");
				yield break;
			}

			foreach (var package in new DirectoryPluginPackageResolver<TPluginManifest>(directory, this.ManifestFileName, this.PluginManifestLoader, SingleFilePluginPackageResolverNoManifestResult.Error).ResolvePluginPackages())
				yield return package;
			yield break;
		}

		foreach (var childDirectory in directory.Directories)
		{
			if (childDirectory.Name.StartsWith(".") && this.IgnoreDotDirectories)
				continue;

			var hadAnyChildPackages = false;
			foreach (var package in this.ResolvePluginPackages(childDirectory))
			{
				hadAnyChildPackages = true;
				yield return package;
			}

			if (isRoot && !hadAnyChildPackages)
				yield return new Error<string>($"Found a folder `{childDirectory.FullName}`, but it does not contain any mods.");
		}
	}
}
