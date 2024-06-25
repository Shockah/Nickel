using System;
using System.IO;

namespace Nanoray.PluginManager;

public abstract class FileSystemInfoImpl<TFileSystemInfo> : IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>
	where TFileSystemInfo : FileSystemInfo
{
	protected TFileSystemInfo FileSystemInfo { get; }

	public string Name
		=> this.FileSystemInfo.Name;

	public string FullName
		=> this.FileSystemInfo.FullName;

	public bool Exists
		=> this.FileSystemInfo.Exists;

	public abstract DirectoryInfoImpl? Parent { get; }

	public FileInfoImpl? AsFile
		=> this as FileInfoImpl ?? (this.FileSystemInfo is FileInfo info ? new FileInfoImpl(info) : null);

	public DirectoryInfoImpl? AsDirectory
		=> this as DirectoryInfoImpl ?? (this.FileSystemInfo is DirectoryInfo info ? new DirectoryInfoImpl(info) : null);

	protected FileSystemInfoImpl(TFileSystemInfo fileSystemInfo)
	{
		this.FileSystemInfo = fileSystemInfo;
	}

	public override string ToString()
		=> this.FileSystemInfo.FullName;

	public override bool Equals(object? obj)
		=> obj is IFileSystemInfo other && this.IsInSameFileSystemType(other) && Equals(this.FullName, other.FullName);

	public override int GetHashCode()
		=> this.FullName.GetHashCode();

	public bool IsInSameFileSystemType(IFileSystemInfo other)
		=> other is IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>;

	public string GetRelativePathTo(IFileSystemInfo other)
	{
		if (other is not IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>)
			throw new ArgumentException("The two file systems are unrelated to each other");
		return Path.GetRelativePath(this.FullName, other.FullName);
	}
}
