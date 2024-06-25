namespace Nanoray.PluginManager;

public abstract class MockFileSystemInfo : IFileSystemInfo<MockFileInfo, MockDirectoryInfo>
{
	public string Name { get; }
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

	protected MockFileSystemInfo(string name, bool exists = true)
	{
		this.Name = name;
		this.Exists = exists;
	}

	public override string ToString()
		=> this.FullName;

	public override bool Equals(object? obj)
		=> obj is IFileSystemInfo other && this.IsInSameFileSystemType(other) && Equals(this.FullName, other.FullName);

	public override int GetHashCode()
		=> this.FullName.GetHashCode();

	public bool IsInSameFileSystemType(IFileSystemInfo other)
		=> other is MockFileSystemInfo;
}
