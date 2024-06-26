using System.IO;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IWritableFileInfo{TFileInfo,TDirectoryInfo}"/> directly exposing the machine's file system.
/// </summary>
public sealed class FileInfoImpl : FileSystemInfoImpl<FileInfo>, IWritableFileInfo<FileInfoImpl, DirectoryInfoImpl>
{
	/// <inheritdoc/>
	public override DirectoryInfoImpl? Parent
		=> this.FileSystemInfo.Directory is { } parent ? new DirectoryInfoImpl(parent) : null;

	/// <summary>
	/// Creates a new instance of <see cref="FileInfoImpl"/>.
	/// </summary>
	/// <param name="info">A file in the machine's file system.</param>
	public FileInfoImpl(FileInfo info) : base(info) { }

	/// <inheritdoc/>
	public Stream OpenRead()
		=> this.FileSystemInfo.OpenRead();

	/// <inheritdoc/>
	public Stream OpenWrite()
	{
		this.Parent?.Create();
		return this.FileSystemInfo.OpenWrite();
	}

	/// <inheritdoc/>
	public void Delete()
	{
		if (this.Exists)
			this.FileSystemInfo.Delete();
	}
}
