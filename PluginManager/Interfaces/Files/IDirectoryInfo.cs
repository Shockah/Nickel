using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager;

public interface IDirectoryInfo : IFileSystemInfo
{
	IEnumerable<IFileSystemInfo> Children { get; }

	IEnumerable<IFileInfo> Files
		=> this.Children
			.Select(c => c.AsFile)
			.Where(f => f is not null)
			.Select(f => f!);

	IEnumerable<IDirectoryInfo> Directories
		=> this.Children
			.Select(c => c.AsDirectory)
			.Where(d => d is not null)
			.Select(d => d!);

	IFileSystemInfo GetChild(string relativePath);
	IFileInfo GetFile(string relativePath);
	IDirectoryInfo GetDirectory(string relativePath);
}

public interface IDirectoryInfo<TFileInfo, TDirectoryInfo> : IFileSystemInfo<TFileInfo, TDirectoryInfo>, IDirectoryInfo
	where TFileInfo : class, IFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	new IEnumerable<IFileSystemInfo<TFileInfo, TDirectoryInfo>> Children { get; }

	new IFileSystemInfo<TFileInfo, TDirectoryInfo> GetChild(string relativePath);

	new TFileInfo GetFile(string relativePath);

	new TDirectoryInfo GetDirectory(string relativePath);

	IEnumerable<IFileSystemInfo> IDirectoryInfo.Children
		=> this.Children;

	IFileSystemInfo IDirectoryInfo.GetChild(string relativePath)
		=> this.GetChild(relativePath);

	IFileInfo IDirectoryInfo.GetFile(string relativePath)
		=> this.GetFile(relativePath);

	IDirectoryInfo IDirectoryInfo.GetDirectory(string relativePath)
		=> this.GetDirectory(relativePath);
}
