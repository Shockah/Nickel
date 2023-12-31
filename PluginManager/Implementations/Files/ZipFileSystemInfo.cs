using System.IO.Compression;

namespace Nanoray.PluginManager;

public abstract class ZipFileSystemInfo : IFileSystemInfo<ZipFileInfo, ZipDirectoryInfo>
{
	public string Name { get; }
	public ZipDirectoryInfo? Parent { get; }
	public bool Exists { get; }
	protected ZipArchive Archive { get; }

	public string FullName
	{
		get
		{
			if (this.Parent is { } parent)
				return $"{parent.FullName}/{this.Name}";
			else
				return $"{(this.Name == "/" ? "" : "/")}{this.Name}";
		}
	}

	public ZipFileInfo? AsFile
		=> this as ZipFileInfo;

	public ZipDirectoryInfo? AsDirectory
		=> this as ZipDirectoryInfo;

	public ZipFileSystemInfo(ZipArchive archive, string name, ZipDirectoryInfo? parent, bool exists = true)
	{
		this.Archive = archive;
		this.Name = name;
		this.Parent = parent;
		this.Exists = exists;
	}

	public bool IsInSameFileSystemType(IFileSystemInfo other)
		=> other is ZipFileSystemInfo;
}
