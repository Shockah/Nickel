using System.IO;

namespace Nanoray.PluginManager;

/// <summary>
/// Describes a file in a file system.
/// </summary>
public interface IFileInfo : IFileSystemInfo
{
	/// <summary>
	/// Creates a read-only <see cref="Stream"/>.
	/// </summary>
	/// <returns>A new read-only <see cref="Stream"/>.</returns>
	Stream OpenRead();
}

/// <summary>
/// Describes a typed file in a file system.
/// </summary>
/// <typeparam name="TFileInfo">The type describing files.</typeparam>
/// <typeparam name="TDirectoryInfo">The type describing directories.</typeparam>
public interface IFileInfo<out TFileInfo, out TDirectoryInfo> : IFileSystemInfo<TFileInfo, TDirectoryInfo>, IFileInfo
	where TFileInfo : class, IFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IDirectoryInfo<TFileInfo, TDirectoryInfo>;
