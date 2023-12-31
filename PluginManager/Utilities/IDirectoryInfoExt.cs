using System.Collections.Generic;

namespace Nanoray.PluginManager;

public static class IDirectoryInfoExt
{
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
