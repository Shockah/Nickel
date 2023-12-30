using System.IO;

namespace Nanoray.PluginManager;

public interface IWritableFileInfo<TFileInfo, TDirectoryInfo> : IFileInfo<TFileInfo, TDirectoryInfo>, IWritableFileSystemInfo<TFileInfo, TDirectoryInfo>
	where TFileInfo : class, IWritableFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IWritableDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	Stream OpenWrite();
}
