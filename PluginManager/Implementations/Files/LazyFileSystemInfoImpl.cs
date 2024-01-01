using System;
using System.IO;

namespace Nanoray.PluginManager;

public sealed class LazyFileSystemInfoImpl : IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>
{
	public string Name { get; }
	public string FullName { get; }

	public bool Exists
		=> File.Exists(this.FullName) || Directory.Exists(this.FullName);

	public FileInfoImpl? AsFile
		=> File.Exists(this.FullName) ? new FileInfoImpl(new FileInfo(this.FullName)) : null;

	public DirectoryInfoImpl? AsDirectory
		=> Directory.Exists(this.FullName) ? new DirectoryInfoImpl(new DirectoryInfo(this.FullName)) : null;

	public DirectoryInfoImpl? Parent
		=> this.AsFile?.Parent ?? this.AsDirectory?.Parent;

	public LazyFileSystemInfoImpl(string name, string fullName)
	{
		this.Name = name;
		this.FullName = fullName;
	}

	public override string ToString()
		=> this.FullName;

	public override bool Equals(object? obj)
		=> obj is IFileSystemInfo other && this.IsInSameFileSystemType(other) && Equals(this.FullName, other.FullName);

	public override int GetHashCode()
		=> this.FullName.GetHashCode();

	public bool IsInSameFileSystemType(IFileSystemInfo other)
		=> other is IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>;

	public string GetRelativePathTo(IFileSystemInfo other)
	{
		if (other is not FileSystemInfoImpl<FileSystemInfo>)
			throw new ArgumentException("The two file systems are unrelated to each other");
		return Path.GetRelativePath(this.FullName, other.FullName);
	}
}
