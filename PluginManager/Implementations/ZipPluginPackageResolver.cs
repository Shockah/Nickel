using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager;

public sealed class ZipPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private FileInfo ZipFile { get; init; }
	private string ManifestFileName { get; init; }
	private IPluginManifestLoader<TPluginManifest> PluginManifestLoader { get; init; }

	public ZipPluginPackageResolver(FileInfo zipFile, string manifestFileName, IPluginManifestLoader<TPluginManifest> pluginManifestLoader)
	{
		this.ZipFile = zipFile;
		this.ManifestFileName = manifestFileName;
		this.PluginManifestLoader = pluginManifestLoader;
	}

	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
	{
		if (!this.ZipFile.Exists)
		{
			yield return new Error<string>($"Could not find a ZIP file at `{this.ZipFile.FullName}`.");
			yield break;
		}

		using var zipStream = this.ZipFile.OpenRead();
		MemoryStream zipMemoryStream = new();
		zipStream.CopyTo(zipMemoryStream);
		ZipArchive archive = new(zipMemoryStream, ZipArchiveMode.Read, leaveOpen: true);

		if (archive.GetEntry(this.ManifestFileName) is not { } manifestEntry)
		{
			archive.Dispose();
			yield return new Error<string>($"Could not find a manifest file `{this.ManifestFileName}` in the ZIP file at `{this.ZipFile.FullName}`.");
			yield break;
		}

		using var stream = manifestEntry.Open();
		var manifest = this.PluginManifestLoader.LoadPluginManifest(zipStream);
		yield return manifest.Match<OneOf<IPluginPackage<TPluginManifest>, Error<string>>>(
			manifest => new ZipPluginPackage<TPluginManifest>(manifest, archive),
			error => new Error<string>($"Could not process the manifest file `{this.ManifestFileName}` in the ZIP file at `{this.ZipFile.FullName}`: {error.Value}")
		);
	}
}
