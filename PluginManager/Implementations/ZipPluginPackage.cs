using System.IO.Compression;

namespace Nanoray.PluginManager;

public sealed class ZipPluginPackage<TPluginManifest> : IPluginPackage<TPluginManifest>
{
	public TPluginManifest Manifest { get; }
	public IDirectoryInfo PackageRoot { get; }

	private ZipArchive Archive { get; }

	public ZipPluginPackage(TPluginManifest manifest, ZipArchive archive)
	{
		this.Manifest = manifest;
		this.PackageRoot = ZipDirectoryInfo.From(archive);
		this.Archive = archive;
	}

	public void Dispose()
		=> this.Archive.Dispose();
}
