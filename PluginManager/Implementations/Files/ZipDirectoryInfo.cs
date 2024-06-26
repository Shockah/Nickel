using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IDirectoryInfo{TFileInfo,TDirectoryInfo}"/> exposing entries in a ZIP file.
/// </summary>
public sealed class ZipDirectoryInfo : ZipFileSystemInfo, IDirectoryInfo<ZipFileInfo, ZipDirectoryInfo>
{
	/// <inheritdoc/>
	public IEnumerable<IFileSystemInfo<ZipFileInfo, ZipDirectoryInfo>> Children
		=> this.LazyChildren.Value;

	private Lazy<List<IFileSystemInfo<ZipFileInfo, ZipDirectoryInfo>>> LazyChildren { get; }

	internal ZipDirectoryInfo(ZipArchive archive, string name, ZipDirectoryInfo? parent, bool exists = true) : base(archive, name, parent, exists)
	{
		this.LazyChildren = new(() => GetDirectChildren(archive, this).ToList());
	}

	/// <summary>
	/// Creates a <see cref="ZipDirectoryInfo"/> exposing entries in the given ZIP archive.
	/// </summary>
	/// <param name="archive">The archive.</param>
	/// <returns>A <see cref="ZipDirectoryInfo"/> representing entries in the given ZIP archive.</returns>
	public static ZipDirectoryInfo From(ZipArchive archive)
		=> new(archive, "/", null);

	private static IEnumerable<IFileSystemInfo<ZipFileInfo, ZipDirectoryInfo>> GetDirectChildren(ZipArchive archive, ZipDirectoryInfo parent)
	{
		var thisPath = parent.FullName;
		if (thisPath.StartsWith('/'))
			thisPath = thisPath[1..];
		if (thisPath.EndsWith('/'))
			thisPath = thisPath[..^1];
		var thisPathComponents = thisPath == "" ? [] : thisPath.Split("/");

		HashSet<string> yieldedDirectoryNames = [];

		foreach (var entry in archive.Entries)
		{
			var entryPath = entry.FullName;
			if (entryPath.StartsWith('/'))
				entryPath = entryPath[1..];
			if (entryPath.EndsWith('/'))
				entryPath = entryPath[..^1];
			var entryPathComponents = entryPath == "" ? [] : entryPath.Split("/");

			if (entryPathComponents.Length <= thisPathComponents.Length)
				continue;

			for (var i = 0; i < thisPathComponents.Length; i++)
				if (entryPathComponents[i] != thisPathComponents[i])
					goto continueEntryEnumeration;

			var infoName = entryPathComponents[thisPathComponents.Length];

			if (entryPathComponents.Length >= thisPathComponents.Length + 2 || entry.FullName.EndsWith('/'))
			{
				if (!yieldedDirectoryNames.Add(infoName))
					continue;
				yield return new ZipDirectoryInfo(archive, infoName, parent);
			}
			else
			{
				yield return new ZipFileInfo(archive, infoName, parent);
			}
			continueEntryEnumeration:;
		}
	}

	/// <inheritdoc/>
	public IFileSystemInfo<ZipFileInfo, ZipDirectoryInfo> GetRelative(string relativePath)
	{
		var split = relativePath.Replace("\\", "/").Split("/");
		var current = this;

		for (var i = 0; i < split.Length - 1; i++)
		{
			if (split[i] == ".")
				continue;

			if (split[i] == "..")
			{
				current = current.Parent ?? new ZipDirectoryInfo(this.Archive, $"{current.Name}{(current.Name.EndsWith("/") ? "" : "/")}..", current, exists: false);
				continue;
			}

			current = current.Children.FirstOrDefault(c => c.Name == split[i])?.AsDirectory ?? new ZipDirectoryInfo(this.Archive, split[i], current, exists: false);
		}

		if (split[^1] == ".")
			return current;

		if (split[^1] == "..")
			return current.Parent ?? new ZipDirectoryInfo(this.Archive, $"{current.Name}{(current.Name.EndsWith("/") ? "" : "/")}..", current, exists: false);

		return current.Children.FirstOrDefault(c => c.Name == split[^1])
			?? new NonExistentZipFileSystemInfo(this.Archive, split[^1], current);
	}

	/// <inheritdoc/>
	public ZipFileInfo GetRelativeFile(string relativePath)
	{
		var child = this.GetRelative(relativePath);
		if (child.AsFile is { } file)
			return file;
		return new ZipFileInfo(this.Archive, child.Name, child.Parent, exists: false);
	}

	/// <inheritdoc/>
	public ZipDirectoryInfo GetRelativeDirectory(string relativePath)
	{
		var child = this.GetRelative(relativePath);
		if (child.AsDirectory is { } directory)
			return directory;
		return new ZipDirectoryInfo(this.Archive, child.Name, child.Parent, exists: false);
	}
}
