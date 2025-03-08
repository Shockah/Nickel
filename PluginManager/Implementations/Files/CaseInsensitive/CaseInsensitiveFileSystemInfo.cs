using System;
using System.Linq;

namespace Nanoray.PluginManager.CaseInsensitive;

/// <summary>
/// An <see cref="IFileSystemInfo{TFileInfo,TDirectoryInfo}"/> wrapper which makes any file operations case insensitive.
/// </summary>
public class CaseInsensitiveFileSystemInfo(IFileSystemInfo wrapped) : IFileSystemInfo<CaseInsensitiveFileInfo, CaseInsensitiveDirectoryInfo>, IFileSystemInfoWrapper
{
	/// <inheritdoc/>
	public IFileSystemInfo Wrapped { get; } = wrapped;

	internal static IFileSystemInfo GetCaseFixed(IFileSystemInfo info)
	{
		while (true)
		{
			if (info is CaseInsensitiveFileSystemInfo caseInsensitiveInfo)
			{
				info = caseInsensitiveInfo.Wrapped;
				continue;
			}

			var root = info.Root;
			if (root.FullName == info.FullName)
				return info;

			var current = root;
			var splitRelativePath = root.GetRelativePathTo(info).Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
			for (var i = 0; i < splitRelativePath.Length; i++)
			{
				if (splitRelativePath[i] == ".")
					continue;

				if (splitRelativePath[i] == "..")
				{
					current = current.Parent!;
					continue;
				}

				if (current.Children.FirstOrDefault(c => c.Name.Equals(splitRelativePath[i], StringComparison.InvariantCultureIgnoreCase)) is not { } child)
					break;

				splitRelativePath[i] = child.Name;

				if (child.AsDirectory is not { } childDirectory)
					break;

				current = childDirectory;
			}

			var newRelativePath = string.Join('/', splitRelativePath);
			return root.GetRelative(newRelativePath);
		}
	}

	/// <inheritdoc/>
	public string Name
		=> this.Wrapped.Name;

	/// <inheritdoc/>
	public string FullName
		=> this.Wrapped.FullName;

	/// <inheritdoc/>
	public bool Exists
		=> GetCaseFixed(this.Wrapped).Exists;
	
	/// <inheritdoc/>
	public CaseInsensitiveFileInfo? AsFile
		=> this as CaseInsensitiveFileInfo ?? (this.Wrapped.AsFile is { } file ? new CaseInsensitiveFileInfo(file) : null);

	/// <inheritdoc/>
	public CaseInsensitiveDirectoryInfo? AsDirectory
		=> this as CaseInsensitiveDirectoryInfo ?? (this.Wrapped.AsDirectory is { } directory ? new CaseInsensitiveDirectoryInfo(directory) : null);
	
	/// <inheritdoc/>
	public CaseInsensitiveDirectoryInfo? Parent
		=> this.Wrapped.Parent is { } parent ? new CaseInsensitiveDirectoryInfo(parent) : null;

	/// <inheritdoc/>
	public bool IsInSameFileSystemType(IFileSystemInfo other)
		=> other is CaseInsensitiveFileSystemInfo info && this.Wrapped.IsInSameFileSystemType(info.Wrapped);

	/// <inheritdoc/>
	public override string ToString()
		=> this.FullName;

	/// <inheritdoc/>
	public override bool Equals(object? obj)
		=> obj is IFileSystemInfo other && this.IsInSameFileSystemType(other) && Equals(this.FullName, other.FullName);

	/// <inheritdoc/>
	public override int GetHashCode()
		=> this.FullName.GetHashCode();
}
