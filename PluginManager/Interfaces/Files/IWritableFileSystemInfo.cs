namespace Nanoray.PluginManager;

/// <summary>
/// Describes a writable file system entry - either a file or a directory.
/// </summary>
/// <seealso cref="IWritableFileInfo"/>
/// <seealso cref="IWritableDirectoryInfo"/>
public interface IWritableFileSystemInfo : IFileSystemInfo
{
	/// <summary>
	/// Deletes this entry from the file system, including any contents.
	/// </summary>
	void Delete();
}

/// <summary>
/// Describes a typed writable file system entry - either a file or a directory.
/// </summary>
/// <typeparam name="TFileInfo">The type describing files.</typeparam>
/// <typeparam name="TDirectoryInfo">The type describing directories.</typeparam>
public interface IWritableFileSystemInfo<out TFileInfo, out TDirectoryInfo> : IFileSystemInfo<TFileInfo, TDirectoryInfo>, IWritableFileSystemInfo
	where TFileInfo : class, IWritableFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IWritableDirectoryInfo<TFileInfo, TDirectoryInfo>;
