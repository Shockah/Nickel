using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nanoray.PluginManager;

public sealed class DirectoryInfoImpl : FileSystemInfoImpl<DirectoryInfo>, IWritableDirectoryInfo<FileInfoImpl, DirectoryInfoImpl>
{
	public override DirectoryInfoImpl? Parent
		=> this.FileSystemInfo.Parent is { } parent ? new DirectoryInfoImpl(parent) : null;

	public IEnumerable<IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>> Children
		=> this.FileSystemInfo.EnumerateFileSystemInfos().Select<FileSystemInfo, IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>>(i =>
		{
			if (i is FileInfo fileInfo)
				return new FileInfoImpl(fileInfo);
			if (i is DirectoryInfo directoryInfo)
				return new DirectoryInfoImpl(directoryInfo);
			throw new InvalidDataException($"Unrecognized type {i.GetType()}");
		});

	public DirectoryInfoImpl(DirectoryInfo info) : base(info)
	{
	}

	public IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl> GetRelative(string relativePath)
		=> new LazyFileSystemInfoImpl(relativePath.Replace("\\", "/").Split("/").Last(), Path.Combine(this.FullName, relativePath));

	public FileInfoImpl GetRelativeFile(string relativePath)
		=> new(new FileInfo(Path.Combine(this.FullName, relativePath)));

	public DirectoryInfoImpl GetRelativeDirectory(string relativePath)
		=> new(new DirectoryInfo(Path.Combine(this.FullName, relativePath)));

	public void Create()
		=> this.FileSystemInfo.Create();

	public void Delete()
		=> this.FileSystemInfo.Delete(recursive: true);
}
