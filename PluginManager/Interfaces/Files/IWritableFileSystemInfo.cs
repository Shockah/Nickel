namespace Nanoray.PluginManager;

public interface IWritableFileSystemInfo<TFileInfo, TDirectoryInfo> : IFileSystemInfo<TFileInfo, TDirectoryInfo>
	where TFileInfo : class, IWritableFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IWritableDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	void Delete();
}
