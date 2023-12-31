using OneOf;
using OneOf.Types;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

public sealed class DirectoryPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IDirectoryInfo Directory { get; }
	private string ManifestFileName { get; }
	private IPluginManifestLoader<TPluginManifest> PluginManifestLoader { get; }
	private SingleFilePluginPackageResolverNoManifestResult NoManifestResult { get; }

	public DirectoryPluginPackageResolver(
		IDirectoryInfo directory,
		string manifestFileName,
		IPluginManifestLoader<TPluginManifest> pluginManifestLoader,
		SingleFilePluginPackageResolverNoManifestResult noManifestResult
	)
	{
		this.Directory = directory;
		this.ManifestFileName = manifestFileName;
		this.PluginManifestLoader = pluginManifestLoader;
		this.NoManifestResult = noManifestResult;
	}

	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
	{
		var manifestFile = this.Directory.GetRelativeFile(this.ManifestFileName);
		if (!manifestFile.Exists)
		{
			if (this.NoManifestResult == SingleFilePluginPackageResolverNoManifestResult.Error)
				yield return new Error<string>($"Could not find a manifest file at `{manifestFile.FullName}`.");
			yield break;
		}

		using var stream = manifestFile.OpenRead();
		var manifest = this.PluginManifestLoader.LoadPluginManifest(stream);
		yield return manifest.Match<OneOf<IPluginPackage<TPluginManifest>, Error<string>>>(
			manifest => new DirectoryPluginPackage<TPluginManifest>(manifest, this.Directory),
			error => new Error<string>($"Could not process the manifest file at `{manifestFile.FullName}`: {error.Value}")
		);
	}
}
