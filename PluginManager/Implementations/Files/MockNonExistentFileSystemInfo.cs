namespace Nanoray.PluginManager;

public sealed class MockNonExistentFileSystemInfo : MockFileSystemInfo, IFileSystemInfo<MockFileInfo, MockDirectoryInfo>
{
	public MockNonExistentFileSystemInfo(string name, MockDirectoryInfo? parent = null) : base(name, exists: false)
	{
		this.Parent = parent;
	}
}
