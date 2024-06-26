using System;

namespace Nanoray.PluginManager;

/// <summary>
/// Hosts extensions for <see cref="IFileSystemInfo"/>.
/// </summary>
public static class FileSystemInfoExt
{
	/// <summary>
	/// Creates a relative path between two entries.
	/// </summary>
	/// <param name="from">The source entry.</param>
	/// <param name="to">The target entry.</param>
	/// <returns>A relative path between two entries.</returns>
	/// <exception cref="ArgumentException">When the two entries are in unrelated file systems.</exception>
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
