using System;

namespace Nanoray.PluginManager;

public abstract class MockFileSystemInfo : IFileSystemInfo<MockFileInfo, MockDirectoryInfo>
{
	public string Name { get; init; }
	public MockDirectoryInfo? Parent { get; internal set; }
	public bool Exists { get; }

	public string FullName
	{
		get
		{
			if (this.Parent is { } parent)
				return $"{parent.FullName}/{this.Name}";
			else
				return $"{(this.Name == "/" ? "" : "/")}{this.Name}";
		}
	}

	public MockFileInfo? AsFile
		=> this as MockFileInfo;

	public MockDirectoryInfo? AsDirectory
		=> this as MockDirectoryInfo;

	public MockFileSystemInfo(string name, bool exists = true)
	{
		this.Name = name;
		this.Exists = exists;
	}

	public string GetRelativePathTo(IFileSystemInfo other)
	{
		if (other is not IFileSystemInfo<MockFileInfo, MockDirectoryInfo>)
			throw new ArgumentException("The two file systems are unrelated to each other");

		var thisRoot = this;
		while (true)
		{
			var next = thisRoot.Parent;
			if (next is null)
				break;
			thisRoot = next;
		}

		var otherRoot = other;
		while (true)
		{
			var next = otherRoot.Parent;
			if (next is null)
				break;
			otherRoot = next;
		}

		if (thisRoot != otherRoot)
			throw new ArgumentException("The two file systems are unrelated to each other");
		var relativePath = other.FullName.Substring(this.FullName.Length);
		if (relativePath.StartsWith("/"))
			relativePath = relativePath.Substring(1);
		return relativePath;
	}
}
