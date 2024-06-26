namespace Nanoray.PluginManager;

/// <summary>
/// A mock <see cref="IFileSystemInfo{TFileInfo,TDirectoryInfo}"/>.
/// </summary>
public abstract class MockFileSystemInfo : IFileSystemInfo<MockFileInfo, MockDirectoryInfo>
{
	/// <inheritdoc/>
	public string Name { get; }
	
	/// <inheritdoc/>
	public MockDirectoryInfo? Parent { get; internal set; }
	
	/// <inheritdoc/>
	public bool Exists { get; }

	/// <inheritdoc/>
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

	/// <inheritdoc/>
	public MockFileInfo? AsFile
		=> this as MockFileInfo;

	/// <inheritdoc/>
	public MockDirectoryInfo? AsDirectory
		=> this as MockDirectoryInfo;

	protected MockFileSystemInfo(string name, bool exists = true)
	{
		this.Name = name;
		this.Exists = exists;
	}

	/// <inheritdoc/>
	public override string ToString()
		=> this.FullName;

	/// <inheritdoc/>
	public override bool Equals(object? obj)
		=> obj is IFileSystemInfo other && this.IsInSameFileSystemType(other) && Equals(this.FullName, other.FullName);

	/// <inheritdoc/>
	public override int GetHashCode()
		=> this.FullName.GetHashCode();

	/// <inheritdoc/>
	public bool IsInSameFileSystemType(IFileSystemInfo other)
		=> other is MockFileSystemInfo;
}
