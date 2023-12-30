using System.Collections.Generic;

namespace Nanoray.PluginManager;

public interface IDirectoryInfo : IFileSystemInfo
{
	IEnumerable<IFileSystemInfo> Children { get; }
}

public interface IDirectoryInfo<TFileInfo, TDirectoryInfo> : IFileSystemInfo<TFileInfo, TDirectoryInfo>, IDirectoryInfo
	where TFileInfo : class, IFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	new IEnumerable<IFileSystemInfo<TFileInfo, TDirectoryInfo>> Children { get; }

	IEnumerable<IFileSystemInfo> IDirectoryInfo.Children
		=> this.Children;
}
