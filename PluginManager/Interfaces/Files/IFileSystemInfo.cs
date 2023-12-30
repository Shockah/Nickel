namespace Nanoray.PluginManager;

public interface IFileSystemInfo<TFileInfo, TDirectoryInfo>
	where TFileInfo : class, IFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	string Name { get; }
	bool Exists { get; }

	TDirectoryInfo? Parent { get; }
	TFileInfo? AsFile { get; }
	TDirectoryInfo? AsDirectory { get; }
}
