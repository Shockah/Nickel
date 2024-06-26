using System.IO.Compression;

namespace Nanoray.PluginManager;

/// <summary>
/// A non-existent <see cref="IFileSystemInfo{TFileInfo,TDirectoryInfo}"/> exposing entries in a ZIP file.
/// </summary>
public sealed class NonExistentZipFileSystemInfo : ZipFileSystemInfo
{
	/// <summary>
	/// Creates a new instance of <see cref="NonExistentZipFileSystemInfo"/>.
	/// </summary>
	/// <param name="archive">The archive.</param>
	/// <param name="name">The entry's name in its <see cref="IFileSystemInfo.Parent"/>.</param>
	/// <param name="parent">The <see cref="IFileSystemInfo.Parent"/> of this entry.</param>
	public NonExistentZipFileSystemInfo(ZipArchive archive, string name, ZipDirectoryInfo? parent = null) : base(archive, name, parent, exists: false)
	{
	}
}
