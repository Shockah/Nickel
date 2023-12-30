using System.IO;

namespace Nanoray.PluginManager;

public interface IFileInfo : IFileSystemInfo
{
	Stream OpenRead();
}

public interface IFileInfo<TFileInfo, TDirectoryInfo> : IFileSystemInfo<TFileInfo, TDirectoryInfo>, IFileInfo
	where TFileInfo : class, IFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IDirectoryInfo<TFileInfo, TDirectoryInfo>
{
}
