using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nanoray.PluginManager;

/// <summary>
/// A mock <see cref="IDirectoryInfo{TFileInfo,TDirectoryInfo}"/>.
/// </summary>
public sealed class MockDirectoryInfo : MockFileSystemInfo, IDirectoryInfo<MockFileInfo, MockDirectoryInfo>
{
	/// <inheritdoc/>
	public IEnumerable<IFileSystemInfo<MockFileInfo, MockDirectoryInfo>> Children { get; }
	
	/// <summary>
	/// Creates a new instance of <see cref="MockDirectoryInfo"/>.
	/// </summary>
	/// <param name="name">The entry's name in its <see cref="IFileSystemInfo.Parent"/>.</param>
	/// <param name="children">The entries contained directly in this directory.</param>
	/// <param name="exists">Whether this file exists.</param>
	public MockDirectoryInfo(string name, ICollection<IFileSystemInfo<MockFileInfo, MockDirectoryInfo>>? children = null, bool exists = true) : base(name, exists)
	{
		this.Children = children ?? [];

		foreach (var child in this.Children)
		{
			if (child is MockFileInfo file)
				file.Parent = this;
			else if (child is MockDirectoryInfo directory)
				directory.Parent = this;
			else if (child is NonExistentMockFileSystemInfo nonExistent)
				nonExistent.Parent = this;
			else
				throw new InvalidDataException($"Unrecognized type {child.GetType()}");
		}
	}

	/// <inheritdoc/>
	public IFileSystemInfo<MockFileInfo, MockDirectoryInfo> GetRelative(string relativePath)
	{
		var split = relativePath.Replace("\\", "/").Split("/", StringSplitOptions.RemoveEmptyEntries);
		var current = this;

		for (var i = 0; i < split.Length - 1; i++)
		{
			if (split[i] == ".")
				continue;

			if (split[i] == "..")
			{
				current = current.Parent ?? new MockDirectoryInfo($"{current.Name}{(current.Name.EndsWith('/') ? "" : "/")}..", exists: false)
				{
					Parent = current
				};
				continue;
			}

			current = current.Children.FirstOrDefault(c => c.Name == split[i])?.AsDirectory ?? new MockDirectoryInfo(split[i], exists: false)
			{
				Parent = current
			};
		}

		if (split[^1] == ".")
			return current;

		if (split[^1] == "..")
			return current.Parent ?? new MockDirectoryInfo($"{current.Name}{(current.Name.EndsWith('/') ? "" : "/")}..", exists: false)
			{
				Parent = current
			};

		return current.Children.FirstOrDefault(c => c.Name == split[^1])
			?? new NonExistentMockFileSystemInfo(split[^1], current);
	}

	/// <inheritdoc/>
	public MockFileInfo GetRelativeFile(string relativePath)
	{
		var child = this.GetRelative(relativePath);
		if (child.AsFile is { } file)
			return file;
		return new MockFileInfo(child.Name, exists: false)
		{
			Parent = child.Parent
		};
	}

	/// <inheritdoc/>
	public MockDirectoryInfo GetRelativeDirectory(string relativePath)
	{
		var child = this.GetRelative(relativePath);
		if (child.AsDirectory is { } directory)
			return directory;
		return new MockDirectoryInfo(child.Name, exists: false)
		{
			Parent = child.Parent
		};
	}
}
