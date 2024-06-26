namespace Nanoray.PluginManager;

/// <summary>
/// Describes a writable directory in a file system.
/// </summary>
public interface IWritableDirectoryInfo : IDirectoryInfo, IWritableFileSystemInfo
{
	/// <summary>
	/// Creates a directory at this path, if one does not yet exist.
	/// </summary>
	void Create();

	/// <summary>
	/// References a file contained directly or indirectly in this directory.
	/// </summary>
	/// <param name="relativePath">The relative path to the file. This can include <c>/</c>, <c>.</c> and <c>..</c> symbols to indicate directory traversal.</param>
	/// <returns>A file relative to this directory.</returns>
	/// <remarks>This file does not need to exist.</remarks>
	new IWritableFileInfo GetRelativeFile(string relativePath);

	/// <summary>
	/// References a directory contained directly or indirectly in this directory.
	/// </summary>
	/// <param name="relativePath">The relative path to the directory. This can include <c>/</c>, <c>.</c> and <c>..</c> symbols to indicate directory traversal.</param>
	/// <returns>A directory relative to this directory.</returns>
	/// <remarks>This directory does not need to exist.</remarks>
	new IWritableDirectoryInfo GetRelativeDirectory(string relativePath);

	/// <inheritdoc/>
	IFileInfo IDirectoryInfo.GetRelativeFile(string relativePath)
		=> this.GetRelativeFile(relativePath);

	/// <inheritdoc/>
	IDirectoryInfo IDirectoryInfo.GetRelativeDirectory(string relativePath)
		=> this.GetRelativeDirectory(relativePath);
}

/// <summary>
/// Describes a typed writable directory in a file system.
/// </summary>
/// <typeparam name="TFileInfo">The type describing files.</typeparam>
/// <typeparam name="TDirectoryInfo">The type describing directories.</typeparam>
public interface IWritableDirectoryInfo<out TFileInfo, out TDirectoryInfo> : IDirectoryInfo<TFileInfo, TDirectoryInfo>, IWritableFileSystemInfo<TFileInfo, TDirectoryInfo>, IWritableDirectoryInfo
	where TFileInfo : class, IWritableFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IWritableDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	/// <summary>
	/// References a file contained directly or indirectly in this directory.
	/// </summary>
	/// <param name="relativePath">The relative path to the file. This can include <c>/</c>, <c>.</c> and <c>..</c> symbols to indicate directory traversal.</param>
	/// <returns>A file relative to this directory.</returns>
	/// <remarks>This file does not need to exist.</remarks>
	new TFileInfo GetRelativeFile(string relativePath);

	/// <summary>
	/// References a directory contained directly or indirectly in this directory.
	/// </summary>
	/// <param name="relativePath">The relative path to the directory. This can include <c>/</c>, <c>.</c> and <c>..</c> symbols to indicate directory traversal.</param>
	/// <returns>A directory relative to this directory.</returns>
	/// <remarks>This directory does not need to exist.</remarks>
	new TDirectoryInfo GetRelativeDirectory(string relativePath);

	/// <inheritdoc cref="IWritableDirectoryInfo.GetRelativeFile"/>
	IFileInfo IDirectoryInfo.GetRelativeFile(string relativePath)
		=> this.GetRelativeFile(relativePath);

	/// <inheritdoc cref="IWritableDirectoryInfo.GetRelativeDirectory"/>
	IDirectoryInfo IDirectoryInfo.GetRelativeDirectory(string relativePath)
		=> this.GetRelativeDirectory(relativePath);

	/// <inheritdoc/>
	IWritableFileInfo IWritableDirectoryInfo.GetRelativeFile(string relativePath)
		=> this.GetRelativeFile(relativePath);
	
	/// <inheritdoc/>
	IWritableDirectoryInfo IWritableDirectoryInfo.GetRelativeDirectory(string relativePath)
		=> this.GetRelativeDirectory(relativePath);
}
