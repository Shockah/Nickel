namespace Nanoray.PluginManager;

public sealed class MockNonExistentFileSystemInfo : IFileSystemInfo<MockFileInfo, MockDirectoryInfo>
{
	public string Name { get; }
	public string FullName { get; }

	public bool Exists
		=> false;

	public MockFileInfo? AsFile
		=> null;

	public MockDirectoryInfo? AsDirectory
		=> null;

	public MockDirectoryInfo? Parent
		=> this.AsFile?.Parent ?? this.AsDirectory?.Parent;

	public MockNonExistentFileSystemInfo(string name, string fullName)
	{
		this.Name = name;
		this.FullName = fullName;
	}
}
