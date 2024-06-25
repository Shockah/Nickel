using System.IO.Compression;

namespace Nanoray.PluginManager;

public sealed class NonExistentZipFileSystemInfo : ZipFileSystemInfo
{
	public NonExistentZipFileSystemInfo(ZipArchive archive, string name, ZipDirectoryInfo? parent = null) : base(archive, name, parent, exists: false)
	{
	}
}
