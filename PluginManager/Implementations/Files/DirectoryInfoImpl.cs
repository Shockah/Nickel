using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nanoray.PluginManager;

public sealed record DirectoryInfoImpl(
	DirectoryInfo DirectoryInfo
) : IWritableDirectoryInfo<FileInfoImpl, DirectoryInfoImpl>
{
	public string Name
		=> this.DirectoryInfo.Name;

	public string FullName
		=> this.DirectoryInfo.FullName;

	public bool Exists
		=> this.DirectoryInfo.Exists;

	public DirectoryInfoImpl? Parent
		=> this.DirectoryInfo.Parent is { } parent ? new DirectoryInfoImpl(parent) : null;

	public IEnumerable<IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>> Children
		=> this.DirectoryInfo.EnumerateFileSystemInfos().Select<FileSystemInfo, IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>>(i =>
		{
			if (i is FileInfo fileInfo)
				return new FileInfoImpl(fileInfo);
			if (i is DirectoryInfo directoryInfo)
				return new DirectoryInfoImpl(directoryInfo);
			throw new InvalidDataException($"Unrecognized type {i.GetType()}");
		});

	public FileInfoImpl? AsFile
		=> null;

	public DirectoryInfoImpl? AsDirectory
		=> this;

	public IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl> GetRelative(string relativePath)
		=> new LazyFileSystemInfoImpl(relativePath.Replace("\\", "/").Split("/").Last(), Path.Combine(this.FullName, relativePath));

	public FileInfoImpl GetRelativeFile(string relativePath)
		=> new(new FileInfo(Path.Combine(this.FullName, relativePath)));

	public DirectoryInfoImpl GetRelativeDirectory(string relativePath)
		=> new(new DirectoryInfo(Path.Combine(this.FullName, relativePath)));

	public void Create()
		=> this.DirectoryInfo.Create();

	public void Delete()
		=> this.DirectoryInfo.Delete(recursive: true);

	public string GetRelativePathTo(IFileSystemInfo other)
	{
		if (other is not IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>)
			throw new ArgumentException("The two file systems are unrelated to each other");
		return Path.GetRelativePath(this.FullName, other.FullName);
	}
}
