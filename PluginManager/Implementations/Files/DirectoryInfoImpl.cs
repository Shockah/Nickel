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

	public void Create()
		=> this.DirectoryInfo.Create();

	public void Delete()
		=> this.DirectoryInfo.Delete(recursive: true);
}
