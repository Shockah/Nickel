using System.Collections.Generic;

namespace Nanoray.PluginManager;

public interface IDirectoryInfo<TFileInfo, TDirectoryInfo> : IFileSystemInfo<TFileInfo, TDirectoryInfo>
	where TFileInfo : class, IFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	IEnumerable<IFileSystemInfo<TFileInfo, TDirectoryInfo>> Children { get; }
}
