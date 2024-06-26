using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IWritableDirectoryInfo{TFileInfo,TDirectoryInfo}"/> directly exposing the machine's file system.
/// </summary>
public sealed class DirectoryInfoImpl : FileSystemInfoImpl<DirectoryInfo>, IWritableDirectoryInfo<FileInfoImpl, DirectoryInfoImpl>
{
	/// <inheritdoc/>
	public override DirectoryInfoImpl? Parent
		=> this.FileSystemInfo.Parent is { } parent ? new DirectoryInfoImpl(parent) : null;

	/// <inheritdoc/>
	public IEnumerable<IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>> Children
		=> this.FileSystemInfo.EnumerateFileSystemInfos().Select<FileSystemInfo, IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>>(i =>
		{
			if (i is FileInfo fileInfo)
				return new FileInfoImpl(fileInfo);
			if (i is DirectoryInfo directoryInfo)
				return new DirectoryInfoImpl(directoryInfo);
			throw new InvalidDataException($"Unrecognized type {i.GetType()}");
		});

	/// <summary>
	/// Creates a new instance of <see cref="DirectoryInfoImpl"/>.
	/// </summary>
	/// <param name="info">A directory in the machine's file system.</param>
	public DirectoryInfoImpl(DirectoryInfo info) : base(info) { }

	/// <inheritdoc/>
	public IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl> GetRelative(string relativePath)
		=> new LazyFileSystemInfoImpl(relativePath.Replace("\\", "/").Split("/").Last(), Path.Combine(this.FullName, relativePath));

	/// <inheritdoc cref="IWritableDirectoryInfo.GetRelativeFile"/>
	public FileInfoImpl GetRelativeFile(string relativePath)
		=> new(new FileInfo(Path.Combine(this.FullName, relativePath)));

	/// <inheritdoc cref="IWritableDirectoryInfo.GetRelativeDirectory"/>
	public DirectoryInfoImpl GetRelativeDirectory(string relativePath)
		=> new(new DirectoryInfo(Path.Combine(this.FullName, relativePath)));

	/// <inheritdoc/>
	public void Create()
		=> this.FileSystemInfo.Create();

	/// <inheritdoc/>
	public void Delete()
	{
		if (this.Exists)
			this.FileSystemInfo.Delete(recursive: true);
	}
}
