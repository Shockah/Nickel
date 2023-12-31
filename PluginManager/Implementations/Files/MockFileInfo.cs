using System.IO;

namespace Nanoray.PluginManager;

public sealed class MockFileInfo : MockFileSystemInfo, IFileInfo<MockFileInfo, MockDirectoryInfo>
{
	private byte[] Data { get; }

	public MockFileInfo(string name, byte[]? data = null, bool exists = true) : base(name, exists)
	{
		this.Data = data ?? [];
	}

	public Stream OpenRead()
		=> this.Exists ? new MemoryStream(this.Data) : throw new FileNotFoundException();
}
