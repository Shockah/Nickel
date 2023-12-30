namespace Nanoray.PluginManager;

public interface IFileSystemInfo
{
	string Name { get; }
	bool Exists { get; }

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
