using System.IO;

namespace Nanoray.PluginManager;

public sealed class MockFileInfo : IFileInfo<MockFileInfo, MockDirectoryInfo>
{
	public string Name { get; init; }
	public MockDirectoryInfo? Parent { get; internal set; }
	public bool Exists { get; }
	private byte[] Data { get; }
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
				throw new InvalidDataException($"The {nameof(MockFileInfo)} has no {nameof(this.RootPath)} nor {nameof(this.Parent)}");
		}
	}

	public MockFileInfo? AsFile
		=> this;

	public MockDirectoryInfo? AsDirectory
		=> null;

	public MockFileInfo(string name, byte[]? data = null)
	{
		this.Name = name;
		this.Exists = true;
		this.Data = data ?? [];
	}

	public MockFileInfo(string rootPath, string name, byte[]? data = null) : this(name, data)
	{
		this.RootPath = rootPath;
	}

	public MockFileInfo(string rootPath, string name, bool exists)
	{
		this.RootPath = rootPath;
		this.Name = name;
		this.Exists = exists;
		this.Data = [];
	}

	public Stream OpenRead()
		=> this.Exists ? new MemoryStream(this.Data) : throw new FileNotFoundException();
}
