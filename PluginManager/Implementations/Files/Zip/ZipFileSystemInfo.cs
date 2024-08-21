using System.IO.Compression;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IFileSystemInfo{TFileInfo,TDirectoryInfo}"/> exposing entries in a ZIP file.
/// </summary>
public abstract class ZipFileSystemInfo : IFileSystemInfo<ZipFileInfo, ZipDirectoryInfo>
{
	/// <inheritdoc/>
	public string Name { get; }
	
	/// <inheritdoc/>
	public ZipDirectoryInfo? Parent { get; }
	
	/// <inheritdoc/>
	public bool Exists { get; }
	
	internal ZipArchive Archive { get; }

	/// <inheritdoc/>
	public string FullName
		=> this.Parent is not null
			? $"{this.Parent.FullName}{(this.Parent.FullName == "/" ? "" : "/")}{this.Name}"
			: $"{(this.Name == "/" ? "" : "/")}{this.Name}";

	/// <inheritdoc/>
	public ZipFileInfo? AsFile
		=> this as ZipFileInfo;

	/// <inheritdoc/>
	public ZipDirectoryInfo? AsDirectory
		=> this as ZipDirectoryInfo;

	internal ZipFileSystemInfo(ZipArchive archive, string name, ZipDirectoryInfo? parent, bool exists = true)
	{
		this.Archive = archive;
		this.Name = name;
		this.Parent = parent;
		this.Exists = exists;
	}

	/// <inheritdoc/>
	public override string ToString()
		=> this.FullName;

	/// <inheritdoc/>
	public override bool Equals(object? obj)
		=> obj is IFileSystemInfo other && this.IsInSameFileSystemType(other) && Equals(this.FullName, other.FullName);

	/// <inheritdoc/>
	public override int GetHashCode()
		=> this.FullName.GetHashCode();

	/// <inheritdoc/>
	public bool IsInSameFileSystemType(IFileSystemInfo other)
		=> other is ZipFileSystemInfo;
}
