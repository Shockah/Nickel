namespace Nanoray.PluginManager;

public interface IFileSystemInfo
{
	string Name { get; }
	string FullName { get; }
	bool Exists { get; }

	bool IsFile
		=> this.AsFile is not null;

	bool IsDirectory
		=> this.AsDirectory is not null;

	IDirectoryInfo? Parent { get; }
	IFileInfo? AsFile { get; }
	IDirectoryInfo? AsDirectory { get; }
}

public interface IFileSystemInfo<TFileInfo, TDirectoryInfo> : IFileSystemInfo
	where TFileInfo : class, IFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	new TDirectoryInfo? Parent { get; }
	new TFileInfo? AsFile { get; }
	new TDirectoryInfo? AsDirectory { get; }

	IDirectoryInfo? IFileSystemInfo.Parent
		=> this.Parent;

	IFileInfo? IFileSystemInfo.AsFile
		=> this.AsFile;

	IDirectoryInfo? IFileSystemInfo.AsDirectory
		=> this.AsDirectory;
}
