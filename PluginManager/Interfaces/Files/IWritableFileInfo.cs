using System.IO;

namespace Nanoray.PluginManager;

public interface IWritableFileInfo : IFileInfo, IWritableFileSystemInfo
{
	Stream OpenWrite();
}

public interface IWritableFileInfo<out TFileInfo, out TDirectoryInfo> : IFileInfo<TFileInfo, TDirectoryInfo>, IWritableFileSystemInfo<TFileInfo, TDirectoryInfo>, IWritableFileInfo
	where TFileInfo : class, IWritableFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IWritableDirectoryInfo<TFileInfo, TDirectoryInfo>
{
}
