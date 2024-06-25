using System;

namespace Nanoray.PluginManager;

public static class FileSystemInfoExt
{
	public static string GetRelativePath(IFileSystemInfo from, IFileSystemInfo to)
	{
		if (!from.IsInSameFileSystemType(to))
			throw new ArgumentException("The two file systems are unrelated to each other");

		var fromRoot = from;
		while (true)
		{
			var next = fromRoot.Parent;
			if (next is null)
				break;
			fromRoot = next;
		}

		var toRoot = to;
		while (true)
		{
			var next = toRoot.Parent;
			if (next is null)
				break;
			toRoot = next;
		}

		if (!Equals(fromRoot, toRoot))
			throw new ArgumentException("The two file systems are unrelated to each other");
		var relativePath = to.FullName[from.FullName.Length..];
		if (relativePath.StartsWith('/'))
			relativePath = relativePath[1..];
		return relativePath;
	}
}
