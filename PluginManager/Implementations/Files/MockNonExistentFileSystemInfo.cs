namespace Nanoray.PluginManager;

public sealed class MockNonExistentFileSystemInfo : IFileSystemInfo<MockFileInfo, MockDirectoryInfo>
{
	public string Name { get; }
	public string FullName { get; }
	public MockDirectoryInfo? Parent { get; }

	public bool Exists
		=> false;

	public MockFileInfo? AsFile
		=> null;

	public MockDirectoryInfo? AsDirectory
		=> null;

	public MockNonExistentFileSystemInfo(string name, string fullName, MockDirectoryInfo? parent = null)
	{
		this.Name = name;
		this.FullName = fullName;
		this.Parent = parent;
	}
}
