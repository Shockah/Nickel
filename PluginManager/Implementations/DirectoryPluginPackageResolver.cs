using OneOf;
using OneOf.Types;
using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager;

public sealed class DirectoryPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IDirectoryInfo Directory { get; }
	private string ManifestFileName { get; }
	private IPluginManifestLoader<TPluginManifest> PluginManifestLoader { get; }

	public DirectoryPluginPackageResolver(IDirectoryInfo directory, string manifestFileName, IPluginManifestLoader<TPluginManifest> pluginManifestLoader)
	{
		this.Directory = directory;
		this.ManifestFileName = manifestFileName;
		this.PluginManifestLoader = pluginManifestLoader;
	}

	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
	{
		var manifestFile = this.Directory.GetFile(this.ManifestFileName);
		if (!manifestFile.Exists)
		{
			yield return new Error<string>($"Could not find a manifest file at `{manifestFile.FullName}`.");
			yield break;
		}

		using var stream = manifestFile.OpenRead();
		var manifest = this.PluginManifestLoader.LoadPluginManifest(stream);
		yield return manifest.Match<OneOf<IPluginPackage<TPluginManifest>, Error<string>>>(
			manifest => new DirectoryPluginPackage<TPluginManifest>(manifest, this.Directory, this.Directory.GetFilesRecursively().ToHashSet()),
			error => new Error<string>($"Could not process the manifest file at `{manifestFile.FullName}`: {error.Value}")
		);
	}
}
