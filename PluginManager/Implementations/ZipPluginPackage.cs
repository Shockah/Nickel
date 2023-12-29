using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Nanoray.PluginManager;

public sealed class ZipPluginPackage<TPluginManifest> : IPluginPackage<TPluginManifest>
{
	public TPluginManifest Manifest { get; init; }
	public IReadOnlySet<string> DataEntries { get; init; }

	private ZipArchive Archive { get; init; }

	public ZipPluginPackage(TPluginManifest manifest, ZipArchive archive)
	{
		this.Manifest = manifest;
		this.Archive = archive;

		this.DataEntries = archive.Entries
			.Select(e => e.Name)
			.ToHashSet();
	}

	public Stream GetDataStream(string entry)
		=> this.Archive.GetEntry(entry)!.Open();
}
