namespace Nanoray.PluginManager;

public interface IWritableDirectoryInfo : IDirectoryInfo, IWritableFileSystemInfo
{
	void Create();

	new IWritableFileInfo GetRelativeFile(string relativePath);

	new IWritableDirectoryInfo GetRelativeDirectory(string relativePath);

	IFileInfo IDirectoryInfo.GetRelativeFile(string relativePath)
		=> this.GetRelativeFile(relativePath);

	IDirectoryInfo IDirectoryInfo.GetRelativeDirectory(string relativePath)
		=> this.GetRelativeDirectory(relativePath);
}

public interface IWritableDirectoryInfo<TFileInfo, TDirectoryInfo> : IDirectoryInfo<TFileInfo, TDirectoryInfo>, IWritableFileSystemInfo<TFileInfo, TDirectoryInfo>, IWritableDirectoryInfo
	where TFileInfo : class, IWritableFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IWritableDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	new TFileInfo GetRelativeFile(string relativePath);

	new TDirectoryInfo GetRelativeDirectory(string relativePath);

	IFileInfo IDirectoryInfo.GetRelativeFile(string relativePath)
		=> this.GetRelativeFile(relativePath);

	IDirectoryInfo IDirectoryInfo.GetRelativeDirectory(string relativePath)
		=> this.GetRelativeDirectory(relativePath);

	IWritableFileInfo IWritableDirectoryInfo.GetRelativeFile(string relativePath)
		=> this.GetRelativeFile(relativePath);

	IWritableDirectoryInfo IWritableDirectoryInfo.GetRelativeDirectory(string relativePath)
		=> this.GetRelativeDirectory(relativePath);
}
