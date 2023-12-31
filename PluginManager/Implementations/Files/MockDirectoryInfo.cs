using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nanoray.PluginManager;

public sealed class MockDirectoryInfo : MockFileSystemInfo, IDirectoryInfo<MockFileInfo, MockDirectoryInfo>
{
	public IEnumerable<IFileSystemInfo<MockFileInfo, MockDirectoryInfo>> Children { get; }

	public MockDirectoryInfo(string name, ICollection<IFileSystemInfo<MockFileInfo, MockDirectoryInfo>>? children = null, bool exists = true) : base(name, exists)
	{
		this.Children = children ?? [];

		foreach (var child in this.Children)
		{
			if (child is MockFileInfo file)
				file.Parent = this;
			else if (child is MockDirectoryInfo directory)
				directory.Parent = this;
			else if (child is MockNonExistentFileSystemInfo nonExistent)
				nonExistent.Parent = this;
			else
				throw new InvalidDataException($"Unrecognized type {child.GetType()}");
		}
	}

	public IFileSystemInfo<MockFileInfo, MockDirectoryInfo> GetRelative(string relativePath)
	{
		var split = relativePath.Replace("\\", "/").Split("/");
		var current = this;

		for (var i = 0; i < split.Length - 1; i++)
		{
			if (split[i] == ".")
				continue;

			if (split[i] == "..")
			{
				current = current.Parent ?? new MockDirectoryInfo($"{current.Name}{(current.Name.EndsWith("/") ? "" : "/")}..", exists: false)
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
			return current.Parent ?? new MockDirectoryInfo($"{current.Name}{(current.Name.EndsWith("/") ? "" : "/")}..", exists: false)
			{
				Parent = current
			};

		return current.Children.FirstOrDefault(c => c.Name == split[^1])
			?? new MockNonExistentFileSystemInfo(split[^1], current);
	}

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
