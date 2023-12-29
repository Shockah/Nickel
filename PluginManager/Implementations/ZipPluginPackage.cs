using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Nanoray.PluginManager;

public sealed class ZipPluginPackage<TPluginManifest> : IPluginPackage<TPluginManifest>
{
	public TPluginManifest Manifest { get; }
	public IReadOnlySet<string> DataEntries { get; }
	private IReadOnlyDictionary<string, string> DataMapping { get; }

	private ZipArchive Archive { get; }

	public ZipPluginPackage(TPluginManifest manifest, ZipArchive archive)
	{
		this.Manifest = manifest;
		this.Archive = archive;

		this.DataMapping = archive.Entries
			.Select(e => new KeyValuePair<string, string>(e.FullName.Replace('\\', '/'), e.FullName))
			.ToImmutableDictionary();

		this.DataEntries = this.DataMapping.Keys.ToHashSet();
	}

	public Stream GetDataStream(string entry) {
		if(!this.DataMapping.TryGetValue(entry, out string? realName)) {
			/* TODO: Exception type! */
			throw new ArgumentException("Entry does not exist.", nameof(entry));
		}

		return this.Archive.GetEntry(realName)!.Open();
	}
}
