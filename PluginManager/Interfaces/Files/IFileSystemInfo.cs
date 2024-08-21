using System;

namespace Nanoray.PluginManager;

/// <summary>
/// Describes a file system entry - either a file or a directory.
/// </summary>
/// <seealso cref="IFileInfo"/>
/// <seealso cref="IDirectoryInfo"/>
public interface IFileSystemInfo
{
	/// <summary>The entry's name in its <see cref="Parent"/>.</summary>
	string Name { get; }
	
	/// <summary>The entry's full path from the root of its file system.</summary>
	string FullName { get; }
	
	/// <summary>Tests whether the entry exists.</summary>
	bool Exists { get; }

	/// <summary>Tests whether the entry is a file.</summary>
	/// <seealso cref="IFileInfo"/>
	bool IsFile
		=> this.AsFile is not null;

	/// <summary>Tests whether the entry is a directory.</summary>
	/// <seealso cref="IDirectoryInfo"/>
	bool IsDirectory
		=> this.AsDirectory is not null;

	/// <summary>The directory this entry is contained in, or <c>null</c> for the root of the file system.</summary>
	IDirectoryInfo? Parent { get; }

	/// <summary>The root directory this entry is contained in.</summary>
	IDirectoryInfo Root
		=> this.Parent?.Root ?? this.AsDirectory ?? throw new InvalidOperationException("This entry has no root directory");
	
	/// <summary>Casts this entry to an <see cref="IFileInfo"/>, if it represents one.</summary>
	IFileInfo? AsFile { get; }
	
	/// <summary>Casts this entry to an <see cref="IDirectoryInfo"/>, if it represents one.</summary>
	IDirectoryInfo? AsDirectory { get; }

	/// <summary>
	/// Tests whether the two entries are in the same file system, which allows using relative paths between the two.
	/// </summary>
	/// <param name="other">The other entry.</param>
	/// <returns>Whether the two entries are in the same file system.</returns>
	bool IsInSameFileSystemType(IFileSystemInfo other);

	/// <summary>
	/// Creates a relative path between two entries.
	/// </summary>
	/// <param name="other">The other entry.</param>
	/// <returns>A relative path between two entries.</returns>
	string GetRelativePathTo(IFileSystemInfo other)
		=> FileSystemInfoExt.GetRelativePath(this, other);
}

/// <summary>
/// Describes a typed file system entry - either a file or a directory.
/// </summary>
/// <typeparam name="TFileInfo">The type describing files.</typeparam>
/// <typeparam name="TDirectoryInfo">The type describing directories.</typeparam>
public interface IFileSystemInfo<out TFileInfo, out TDirectoryInfo> : IFileSystemInfo
	where TFileInfo : class, IFileInfo<TFileInfo, TDirectoryInfo>
	where TDirectoryInfo : class, IDirectoryInfo<TFileInfo, TDirectoryInfo>
{
	/// <inheritdoc cref="IFileSystemInfo.Parent"/>
	new TDirectoryInfo? Parent { get; }
	
	/// <inheritdoc cref="IFileSystemInfo.Root"/>
	new TDirectoryInfo Root
		=> this.Parent?.Root ?? this.AsDirectory ?? throw new InvalidOperationException("This entry has no root directory");
	
	/// <inheritdoc cref="IFileSystemInfo.AsFile"/>
	new TFileInfo? AsFile { get; }
	
	/// <inheritdoc cref="IFileSystemInfo.AsDirectory"/>
	new TDirectoryInfo? AsDirectory { get; }
	
	/// <inheritdoc/>
	IDirectoryInfo? IFileSystemInfo.Parent
		=> this.Parent;
	
	/// <inheritdoc/>
	IDirectoryInfo IFileSystemInfo.Root
		=> this.Root;

	/// <inheritdoc/>
	IFileInfo? IFileSystemInfo.AsFile
		=> this.AsFile;

	/// <inheritdoc/>
	IDirectoryInfo? IFileSystemInfo.AsDirectory
		=> this.AsDirectory;
}
