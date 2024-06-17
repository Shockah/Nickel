namespace Nanoray.PluginManager;

public interface IWritableFileSystemInfo : IFileSystemInfo
{
	void Delete();
}

public interface IWritableFileSystemInfo<TFileInfo, TDirectoryInfo> : IFileSystemInfo<TFileInfo, TDirectoryInfo>, IWritableFileSystemInfo
	where TFileInfo : class, IWritableFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IWritableDirectoryInfo<TFileInfo, TDirectoryInfo>
{
}
