using System.IO;
using System.IO.Compression;

namespace Nanoray.PluginManager;

public sealed class ZipFileInfo : ZipFileSystemInfo, IFileInfo<ZipFileInfo, ZipDirectoryInfo>
{
	internal ZipFileInfo(ZipArchive archive, string name, ZipDirectoryInfo? parent, bool exists = true) : base(archive, name, parent, exists)
	{
	}

	public Stream OpenRead()
	{
		var entryName = this.FullName;
		if (entryName.StartsWith("/"))
			entryName = entryName.Substring(1);
		var entry = this.Archive.GetEntry(entryName) ?? throw new FileNotFoundException("File does not exist", entryName);
		return entry.Open();
	}
}
