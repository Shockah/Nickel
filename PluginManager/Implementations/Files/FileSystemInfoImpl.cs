using System;
using System.IO;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IFileSystemInfo{TFileInfo,TDirectoryInfo}"/> directly exposing the machine's file system.
/// </summary>
public abstract class FileSystemInfoImpl<TFileSystemInfo> : IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>
	where TFileSystemInfo : FileSystemInfo
{
	internal TFileSystemInfo FileSystemInfo { get; }

	/// <inheritdoc/>
	public string Name
		=> this.FileSystemInfo.Name;

	/// <inheritdoc/>
	public string FullName
		=> this.FileSystemInfo.FullName;

	/// <inheritdoc/>
	public bool Exists
		=> this.FileSystemInfo.Exists;

	/// <inheritdoc/>
	public abstract DirectoryInfoImpl? Parent { get; }

	/// <inheritdoc/>
	public FileInfoImpl? AsFile
		=> this as FileInfoImpl ?? (this.FileSystemInfo is FileInfo info ? new FileInfoImpl(info) : null);

	/// <inheritdoc/>
	public DirectoryInfoImpl? AsDirectory
		=> this as DirectoryInfoImpl ?? (this.FileSystemInfo is DirectoryInfo info ? new DirectoryInfoImpl(info) : null);

	internal FileSystemInfoImpl(TFileSystemInfo fileSystemInfo)
	{
		this.FileSystemInfo = fileSystemInfo;
	}

	/// <inheritdoc/>
	public override string ToString()
		=> this.FileSystemInfo.FullName;

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
		if (other is not IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>)
			throw new ArgumentException("The two file systems are unrelated to each other");
		return Path.GetRelativePath(this.FullName, other.FullName);
	}
}
