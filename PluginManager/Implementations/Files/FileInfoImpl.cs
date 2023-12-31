using System.IO;

namespace Nanoray.PluginManager;

public sealed class FileInfoImpl : FileSystemInfoImpl<FileInfo>, IWritableFileInfo<FileInfoImpl, DirectoryInfoImpl>
{
	public override DirectoryInfoImpl? Parent
		=> this.FileSystemInfo.Directory is { } parent ? new DirectoryInfoImpl(parent) : null;

	public FileInfoImpl(FileInfo info) : base(info)
	{
	}

	public Stream OpenRead()
		=> this.FileSystemInfo.OpenRead();

	public Stream OpenWrite()
		=> this.FileSystemInfo.OpenWrite();

	public void Delete()
		=> this.FileSystemInfo.Delete();
}
