namespace Nanoray.PluginManager;

public sealed class MockNonExistentFileSystemInfo : MockFileSystemInfo
{
	public MockNonExistentFileSystemInfo(string name, MockDirectoryInfo? parent = null) : base(name, exists: false)
	{
		this.Parent = parent;
	}
}
