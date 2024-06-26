using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// Hosts extensions for <see cref="IDirectoryInfo"/>.
/// </summary>
public static class IDirectoryInfoExt
{
	/// <summary>
	/// Enumerates all files in the directory recursively.
	/// </summary>
	/// <param name="self">The directory.</param>
	/// <returns>An enumerable over all files in the directory.</returns>
	public static IEnumerable<IFileInfo> GetFilesRecursively(this IDirectoryInfo self)
	{
		foreach (var child in self.Children)
		{
			if (child.AsFile is { } file)
				yield return file;
			else if (child.AsDirectory is { } directory)
				foreach (var nestedFile in directory.GetFilesRecursively())
					yield return nestedFile;
		}
	}

	/// <summary>
	/// Enumerates all files in the directory recursively.
	/// </summary>
	/// <param name="self">The directory.</param>
	/// <returns>An enumerable over all files in the directory.</returns>
	public static IEnumerable<TFileInfo> GetFilesRecursively<TFileInfo, TDirectoryInfo>(this IDirectoryInfo<TFileInfo, TDirectoryInfo> self)
		where TFileInfo : class, IFileInfo<TFileInfo, TDirectoryInfo>
		where TDirectoryInfo : class, IDirectoryInfo<TFileInfo, TDirectoryInfo>
	{
		foreach (var child in self.Children)
		{
			if (child.AsFile is { } file)
				yield return file;
			else if (child.AsDirectory is { } directory)
				foreach (var nestedFile in directory.GetFilesRecursively())
					yield return nestedFile;
		}
	}
}
