using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nanoray.PluginManager;

public sealed class DirectoryPluginPackage<TPluginManifest> : IDirectoryPluginPackage<TPluginManifest>
{
	public TPluginManifest Manifest { get; init; }
	public IDirectoryInfo Directory { get; init; }
	public IReadOnlySet<string> DataEntries { get; init; }

	private IReadOnlySet<IFileInfo> Files { get; }

	public DirectoryPluginPackage(TPluginManifest manifest, IDirectoryInfo directory, IReadOnlySet<IFileInfo> files)
	{
		this.Manifest = manifest;
		this.Directory = directory;
		this.Files = files;

		Uri directoryUri = new(directory.FullName + Path.DirectorySeparatorChar);
		this.DataEntries = this.Files
			.Select(f =>
			{
				Uri uri = new(f.FullName);
				return directoryUri.MakeRelativeUri(uri).ToString();
			})
			.ToHashSet();
	}

	public Stream GetDataStream(string entry)
		=> this.Directory.GetRelative(entry).AsFile?.OpenRead() ?? throw new FileNotFoundException();
}
