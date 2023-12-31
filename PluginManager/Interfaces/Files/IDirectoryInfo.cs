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

	IFileSystemInfo GetRelative(string relativePath);
	IFileInfo GetRelativeFile(string relativePath);
	IDirectoryInfo GetRelativeDirectory(string relativePath);
}

public interface IDirectoryInfo<TFileInfo, TDirectoryInfo> : IFileSystemInfo<TFileInfo, TDirectoryInfo>, IDirectoryInfo
	where TFileInfo : class, IFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	new IEnumerable<IFileSystemInfo<TFileInfo, TDirectoryInfo>> Children { get; }

	new IFileSystemInfo<TFileInfo, TDirectoryInfo> GetRelative(string relativePath);

	new TFileInfo GetRelativeFile(string relativePath);

	new TDirectoryInfo GetRelativeDirectory(string relativePath);

	IEnumerable<IFileSystemInfo> IDirectoryInfo.Children
		=> this.Children;

	IFileSystemInfo IDirectoryInfo.GetRelative(string relativePath)
		=> this.GetRelative(relativePath);

	IFileInfo IDirectoryInfo.GetRelativeFile(string relativePath)
		=> this.GetRelativeFile(relativePath);

	IDirectoryInfo IDirectoryInfo.GetRelativeDirectory(string relativePath)
		=> this.GetRelativeDirectory(relativePath);
}
