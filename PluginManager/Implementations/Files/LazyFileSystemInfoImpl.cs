using System;
using System.IO;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IFileSystemInfo{TFileInfo,TDirectoryInfo}"/> directly exposing the machine's file system.
/// The actual type of the entry is lazily evaluated when needed.
/// </summary>
public sealed class LazyFileSystemInfoImpl : IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>
{
	/// <inheritdoc/>
	public string Name { get; }
	
	/// <inheritdoc/>
	public string FullName { get; }

	/// <inheritdoc/>
	public bool Exists
		=> File.Exists(this.FullName) || Directory.Exists(this.FullName);

	/// <inheritdoc/>
	public FileInfoImpl? AsFile
		=> File.Exists(this.FullName) ? new FileInfoImpl(new FileInfo(this.FullName)) : null;

	/// <inheritdoc/>
	public DirectoryInfoImpl? AsDirectory
		=> Directory.Exists(this.FullName) ? new DirectoryInfoImpl(new DirectoryInfo(this.FullName)) : null;

	/// <inheritdoc/>
	public DirectoryInfoImpl? Parent
		=> this.AsFile?.Parent ?? this.AsDirectory?.Parent;

	/// <summary>
	/// Creates a new instance of <see cref="LazyFileSystemInfoImpl"/>.
	/// </summary>
	/// <param name="name">The entry's name in its <see cref="Parent"/>.</param>
	/// <param name="fullName">The entry's full path from the root of its file system.</param>
	public LazyFileSystemInfoImpl(string name, string fullName)
	{
		this.Name = name;
		this.FullName = fullName;
	}

	/// <inheritdoc/>
	public override string ToString()
		=> this.FullName;

	/// <inheritdoc/>
	public override bool Equals(object? obj)
		=> obj is IFileSystemInfo other && this.IsInSameFileSystemType(other) && Equals(this.FullName, other.FullName);

	/// <inheritdoc/>
	public override int GetHashCode()
		=> this.FullName.GetHashCode();

	/// <inheritdoc/>
	public bool IsInSameFileSystemType(IFileSystemInfo other)
		=> other is IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>;

	/// <inheritdoc/>
	public string GetRelativePathTo(IFileSystemInfo other)
	{
		if (other is not FileSystemInfoImpl<FileSystemInfo>)
			throw new ArgumentException("The two file systems are unrelated to each other");
		return Path.GetRelativePath(this.FullName, other.FullName);
	}
}
