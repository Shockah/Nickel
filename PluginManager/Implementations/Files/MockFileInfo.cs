using System.IO;

namespace Nanoray.PluginManager;

public sealed class MockFileInfo : IFileInfo<MockFileInfo, MockDirectoryInfo>
{
	public string Name { get; init; }
	public MockDirectoryInfo? Parent { get; internal set; }
	public bool Exists { get; }
	private byte[] Data { get; }

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
		=> this;

	public MockDirectoryInfo? AsDirectory
		=> null;

	public MockFileInfo(string name, byte[]? data = null)
	{
		this.Name = name;
		this.Exists = true;
		this.Data = data ?? [];
	}

	public MockFileInfo(string name, bool exists)
	{
		this.Name = name;
		this.Exists = exists;
		this.Data = [];
	}

	public Stream OpenRead()
		=> this.Exists ? new MemoryStream(this.Data) : throw new FileNotFoundException();
}
