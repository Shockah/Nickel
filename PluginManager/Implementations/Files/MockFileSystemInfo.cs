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
				return $"{parent.FullName}{(parent.FullName == "/" ? "" : "/")}{this.Name}";
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

	public bool IsInSameFileSystemType(IFileSystemInfo other)
		=> other is MockFileSystemInfo;
}
