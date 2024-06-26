using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager;

/// <summary>
/// Describes a directory in a file system.
/// </summary>
public interface IDirectoryInfo : IFileSystemInfo
{
	/// <summary>An enumerator for all entries contained directly in this directory.</summary>
	IEnumerable<IFileSystemInfo> Children { get; }

	/// <summary>An enumerator for all files contained directly in this directory.</summary>
	IEnumerable<IFileInfo> Files
		=> this.Children
			.Select(c => c.AsFile)
			.Where(f => f is not null)
			.Select(f => f!);

	/// <summary>An enumerator for all directories contained directly in this directory.</summary>
	IEnumerable<IDirectoryInfo> Directories
		=> this.Children
			.Select(c => c.AsDirectory)
			.Where(d => d is not null)
			.Select(d => d!);

	/// <summary>
	/// References an entry contained directly or indirectly in this directory.
	/// </summary>
	/// <param name="relativePath">The relative path to the entry. This can include <c>/</c>, <c>.</c> and <c>..</c> symbols to indicate directory traversal.</param>
	/// <returns>An entry relative to this directory.</returns>
	/// <remarks>This entry does not need to exist.</remarks>
	IFileSystemInfo GetRelative(string relativePath);
	
	/// <summary>
	/// References a file contained directly or indirectly in this directory.
	/// </summary>
	/// <param name="relativePath">The relative path to the file. This can include <c>/</c>, <c>.</c> and <c>..</c> symbols to indicate directory traversal.</param>
	/// <returns>A file relative to this directory.</returns>
	/// <remarks>This file does not need to exist.</remarks>
	IFileInfo GetRelativeFile(string relativePath);
	
	/// <summary>
	/// References a directory contained directly or indirectly in this directory.
	/// </summary>
	/// <param name="relativePath">The relative path to the directory. This can include <c>/</c>, <c>.</c> and <c>..</c> symbols to indicate directory traversal.</param>
	/// <returns>A directory relative to this directory.</returns>
	/// <remarks>This directory does not need to exist.</remarks>
	IDirectoryInfo GetRelativeDirectory(string relativePath);
}

/// <summary>
/// Describes a typed directory in a file system.
/// </summary>
/// <typeparam name="TFileInfo">The type describing files.</typeparam>
/// <typeparam name="TDirectoryInfo">The type describing directories.</typeparam>
public interface IDirectoryInfo<out TFileInfo, out TDirectoryInfo> : IFileSystemInfo<TFileInfo, TDirectoryInfo>, IDirectoryInfo
	where TFileInfo : class, IFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	/// <summary>An enumerator for all entries contained directly in this directory.</summary>
	new IEnumerable<IFileSystemInfo<TFileInfo, TDirectoryInfo>> Children { get; }

	/// <summary>
	/// References an entry contained directly or indirectly in this directory.
	/// </summary>
	/// <param name="relativePath">The relative path to the entry. This can include <c>/</c>, <c>.</c> and <c>..</c> symbols to indicate directory traversal.</param>
	/// <returns>An entry relative to this directory.</returns>
	/// <remarks>This entry does not need to exist.</remarks>
	new IFileSystemInfo<TFileInfo, TDirectoryInfo> GetRelative(string relativePath);

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

	/// <inheritdoc/>
	IEnumerable<IFileSystemInfo> IDirectoryInfo.Children
		=> this.Children;

	/// <inheritdoc/>
	IFileSystemInfo IDirectoryInfo.GetRelative(string relativePath)
		=> this.GetRelative(relativePath);

	/// <inheritdoc/>
	IFileInfo IDirectoryInfo.GetRelativeFile(string relativePath)
		=> this.GetRelativeFile(relativePath);

	/// <inheritdoc/>
	IDirectoryInfo IDirectoryInfo.GetRelativeDirectory(string relativePath)
		=> this.GetRelativeDirectory(relativePath);
}
