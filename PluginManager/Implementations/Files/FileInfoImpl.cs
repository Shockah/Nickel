using System;
using System.IO;

namespace Nanoray.PluginManager;

public sealed record FileInfoImpl(
	FileInfo FileInfo
) : IWritableFileInfo<FileInfoImpl, DirectoryInfoImpl>
{
	public string Name
		=> this.FileInfo.Name;

	public string FullName
		=> this.FileInfo.FullName;

	public bool Exists
		=> this.FileInfo.Exists;

	public DirectoryInfoImpl? Parent
		=> this.FileInfo.Directory is { } parent ? new DirectoryInfoImpl(parent) : null;

	public FileInfoImpl? AsFile
		=> this;

	public DirectoryInfoImpl? AsDirectory
		=> null;

	public Stream OpenRead()
		=> this.FileInfo.OpenRead();

	public Stream OpenWrite()
		=> this.FileInfo.OpenWrite();

	public void Delete()
		=> this.FileInfo.Delete();

	public string GetRelativePathTo(IFileSystemInfo other)
	{
		if (other is not IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>)
			throw new ArgumentException("The two file systems are unrelated to each other");
		return Path.GetRelativePath(this.FullName, other.FullName);
	}
}
