using System.Collections.Generic;
using System.IO;
using System.Linq;
using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager;

public sealed class DirectoryPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private DirectoryInfo Directory { get; init; }
	private string ManifestFileName { get; init; }
	private IPluginManifestLoader<TPluginManifest> PluginManifestLoader { get; init; }

	public DirectoryPluginPackageResolver(DirectoryInfo directory, string manifestFileName, IPluginManifestLoader<TPluginManifest> pluginManifestLoader)
	{
		this.Directory = directory;
		this.ManifestFileName = manifestFileName;
		this.PluginManifestLoader = pluginManifestLoader;
	}

	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
	{
		FileInfo manifestFile = new(Path.Combine(this.Directory.FullName, this.ManifestFileName));
		if (!manifestFile.Exists)
		{
			yield return new Error<string>($"Could not find a manifest file at `{manifestFile.FullName}`.");
			yield break;
		}

		using var stream = manifestFile.OpenRead();
		var manifest = this.PluginManifestLoader.LoadPluginManifest(stream);
		yield return manifest.Match<OneOf<IPluginPackage<TPluginManifest>, Error<string>>>(
			manifest => new DirectoryPluginPackage<TPluginManifest>(manifest, this.Directory, this.Directory.EnumerateFiles("*", SearchOption.AllDirectories).ToHashSet()),
			error => new Error<string>($"Could not process the manifest file at `{manifestFile.FullName}`: {error.Value}")
		);
	}
}
