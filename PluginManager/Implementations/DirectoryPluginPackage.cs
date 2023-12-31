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

		this.DataEntries = this.Files
			.Select(f => f.FullName.StartsWith(directory.FullName) ? f.FullName.Substring(directory.FullName.Length + 1) : Path.GetRelativePath(directory.FullName, f.FullName))
			.ToHashSet();
	}

	public Stream GetDataStream(string entry)
		=> this.Directory.GetChild(entry).AsFile?.OpenRead() ?? throw new FileNotFoundException();
}
