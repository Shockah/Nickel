using System.IO;

namespace Nanoray.PluginManager;

public sealed record FileInfoImpl(
	FileInfo FileInfo
) : IWritableFileInfo<FileInfoImpl, DirectoryInfoImpl>
{
	public string Name
		=> this.FileInfo.Name;

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
}
