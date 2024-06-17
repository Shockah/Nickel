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
	{
		this.Parent?.Create();
		return this.FileSystemInfo.OpenWrite();
	}

	public void Delete()
	{
		if (this.Exists)
			this.FileSystemInfo.Delete();
	}
}
