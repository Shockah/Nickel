namespace Nanoray.PluginManager;

public interface IWritableDirectoryInfo<TFileInfo, TDirectoryInfo> : IDirectoryInfo<TFileInfo, TDirectoryInfo>, IWritableFileSystemInfo<TFileInfo, TDirectoryInfo>
	where TFileInfo : class, IWritableFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IWritableDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	void Create();
}
