using OneOf.Types;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginPackageResolver{TPluginManifest}"/> which resolves a single plugin from a directory.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public sealed class DirectoryPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private readonly IDirectoryInfo Directory;
	private readonly string ManifestFileName;
	private readonly IPluginManifestLoader<TPluginManifest> PluginManifestLoader;
	private readonly SingleFilePluginPackageResolverNoManifestResult NoManifestResult;

	/// <summary>
	/// Creates a new <see cref="DirectoryPluginPackageResolver{TPluginManifest}"/>.
	/// </summary>
	/// <param name="directory">The directory.</param>
	/// <param name="manifestFileName">The manifest file name to look for.</param>
	/// <param name="pluginManifestLoader">The plugin manifest loader.</param>
	/// <param name="noManifestResult">The behavior when no manifest file is found.</param>
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

	/// <inheritdoc/>
	public IEnumerable<PluginPackageResolveResult<TPluginManifest>> ResolvePluginPackages()
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
		yield return manifest.Match<PluginPackageResolveResult<TPluginManifest>>(
			manifest => new PluginPackageResolveResult<TPluginManifest>.Success
			{
				Package = new DirectoryPluginPackage<TPluginManifest>(manifest, this.Directory),
				Warnings = []
			},
			error => new Error<string>($"Could not process the manifest file at `{manifestFile.FullName}`: {error.Value}")
		);
	}
}
