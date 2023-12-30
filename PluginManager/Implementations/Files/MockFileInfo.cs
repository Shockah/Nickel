using System.IO;

namespace Nanoray.PluginManager;

public sealed class MockFileInfo : IFileInfo<MockFileInfo, MockDirectoryInfo>
{
	public string Name { get; init; }
	public MockDirectoryInfo? Parent { get; internal set; }
	private byte[] Data { get; }

	public bool Exists
		=> true;

	public MockFileInfo? AsFile
		=> this;

	public MockDirectoryInfo? AsDirectory
		=> null;

	public MockFileInfo(string name, byte[]? data = null)
	{
		this.Name = name;
		this.Data = data ?? [];
	}

	public Stream OpenRead()
		=> new MemoryStream(this.Data);
}
