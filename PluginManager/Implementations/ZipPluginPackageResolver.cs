using OneOf;
using OneOf.Types;
using System.Collections.Generic;
using System.IO.Compression;

namespace Nanoray.PluginManager;

public sealed class ZipPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IFileInfo ZipFile { get; }
	private string ManifestFileName { get; }
	private IPluginManifestLoader<TPluginManifest> PluginManifestLoader { get; }
	private SingleFilePluginPackageResolverNoManifestResult NoManifestResult { get; }

	public ZipPluginPackageResolver(
		IFileInfo zipFile,
		string manifestFileName,
		IPluginManifestLoader<TPluginManifest> pluginManifestLoader,
		SingleFilePluginPackageResolverNoManifestResult noManifestResult
	)
	{
		this.ZipFile = zipFile;
		this.ManifestFileName = manifestFileName;
		this.PluginManifestLoader = pluginManifestLoader;
		this.NoManifestResult = noManifestResult;
	}

	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
	{
		if (!this.ZipFile.Exists)
		{
			yield return new Error<string>($"Could not find a ZIP file at `{this.ZipFile.FullName}`.");
			yield break;
		}

		ZipArchive archive = new(this.ZipFile.OpenRead(), ZipArchiveMode.Read, leaveOpen: true);

		if (archive.GetEntry(this.ManifestFileName) is not { } manifestEntry)
		{
			archive.Dispose();
			if (this.NoManifestResult == SingleFilePluginPackageResolverNoManifestResult.Error)
				yield return new Error<string>($"Could not find a manifest file `{this.ManifestFileName}` in the ZIP file at `{this.ZipFile.FullName}`.");
			yield break;
		}

		using var manifestStream = manifestEntry.Open();
		var manifest = this.PluginManifestLoader.LoadPluginManifest(manifestStream);
		yield return manifest.Match<OneOf<IPluginPackage<TPluginManifest>, Error<string>>>(
			manifest => new ZipPluginPackage<TPluginManifest>(manifest, archive),
			error => new Error<string>($"Could not process the manifest file `{this.ManifestFileName}` in the ZIP file at `{this.ZipFile.FullName}`: {error.Value}")
		);
	}
}
