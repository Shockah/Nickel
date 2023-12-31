using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nanoray.PluginManager;

public sealed class MockDirectoryInfo : IDirectoryInfo<MockFileInfo, MockDirectoryInfo>
{
	public string Name { get; init; }
	public MockDirectoryInfo? Parent { get; internal set; }
	public bool Exists { get; }
	public IEnumerable<IFileSystemInfo<MockFileInfo, MockDirectoryInfo>> Children { get; }
	private string? RootPath { get; }

	public string FullName
	{
		get
		{
			if (this.RootPath is { } rootPath)
				return $"{rootPath}{this.Name}";
			else if (this.Parent is { } parent)
				return $"{parent.FullName}/{this.Name}";
			else
				throw new InvalidDataException($"The {nameof(MockDirectoryInfo)} has no {nameof(this.RootPath)} nor {nameof(this.Parent)}");
		}
	}

	public MockFileInfo? AsFile
		=> null;

	public MockDirectoryInfo? AsDirectory
		=> this;

	public MockDirectoryInfo(string? rootPath, string name, bool exists)
	{
		this.RootPath = rootPath;
		this.Name = name;
		this.Exists = exists;
		this.Children = [];
	}

	public MockDirectoryInfo(string name, ICollection<IFileSystemInfo<MockFileInfo, MockDirectoryInfo>> children)
	{
		this.Name = name;
		this.Exists = true;
		this.Children = children;

		foreach (var child in children)
		{
			if (child is MockFileInfo file)
				file.Parent = this;
			else if (child is MockDirectoryInfo directory)
				directory.Parent = this;
			else
				throw new InvalidDataException($"Unrecognized type {child.GetType()}");
		}
	}

	public MockDirectoryInfo(string rootPath, string name, ICollection<IFileSystemInfo<MockFileInfo, MockDirectoryInfo>> children) : this(name, children)
	{
		this.RootPath = rootPath;
	}

	public IFileSystemInfo<MockFileInfo, MockDirectoryInfo> GetChild(string relativePath)
	{
		// TODO: stop using Path methods in mocks
		var currentFullPath = Path.GetFullPath(this.FullName).Replace("\\", "/");
		if (!currentFullPath.EndsWith("/"))
			currentFullPath = $"{currentFullPath}/";

		var newFullPath = Path.GetFullPath(Path.Combine(this.FullName, relativePath)).Replace("\\", "/");

		var split = newFullPath.Substring(currentFullPath.Length).Replace("\\", "/").Split("/");
		if (!newFullPath.StartsWith(currentFullPath))
			return new MockNonExistentFileSystemInfo(split[^1], newFullPath);

		var current = this;
		for (var i = 0; i < split.Length - 1; i++)
		{
			var splitPart = split[i];
			var next = current.Children.FirstOrDefault(c => c.Name == splitPart)?.AsDirectory;
			current = next ?? new MockDirectoryInfo(rootPath: $"{current.FullName}/", name: splitPart, exists: false)
			{
				Parent = current
			};
		}

		var last = current.Children.FirstOrDefault(c => c.Name == split[^1]);
		return last ?? new MockNonExistentFileSystemInfo(split[^1], newFullPath, current);
	}

	public MockFileInfo GetFile(string relativePath)
	{
		var child = this.GetChild(relativePath);
		if (child.AsFile is { } file)
			return file;
		return new MockFileInfo(rootPath: $"{this.RootPath}/", name: child.Name, exists: false);
	}

	public MockDirectoryInfo GetDirectory(string relativePath)
	{
		var child = this.GetChild(relativePath);
		if (child.AsDirectory is { } directory)
			return directory;
		return new MockDirectoryInfo(rootPath: $"{this.RootPath}/", name: child.Name, exists: false);
	}
}
