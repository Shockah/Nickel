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
			if (this.Parent != null)
				return $"{this.Parent.FullName}{(this.Parent.FullName == "/" ? "" : "/")}{this.Name}";
			else
				return $"{(this.Name == "/" ? "" : "/")}{this.Name}";
		}
	}

	public ZipFileInfo? AsFile
		=> this as ZipFileInfo;

	public ZipDirectoryInfo? AsDirectory
		=> this as ZipDirectoryInfo;

	protected ZipFileSystemInfo(ZipArchive archive, string name, ZipDirectoryInfo? parent, bool exists = true)
	{
		this.Archive = archive;
		this.Name = name;
		this.Parent = parent;
		this.Exists = exists;
	}

	public override string ToString()
		=> this.FullName;

	public override bool Equals(object? obj)
		=> obj is IFileSystemInfo other && this.IsInSameFileSystemType(other) && Equals(this.FullName, other.FullName);

	public override int GetHashCode()
		=> this.FullName.GetHashCode();

	public bool IsInSameFileSystemType(IFileSystemInfo other)
		=> other is ZipFileSystemInfo;
}
