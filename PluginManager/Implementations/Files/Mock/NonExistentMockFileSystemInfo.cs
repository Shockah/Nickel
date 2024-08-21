namespace Nanoray.PluginManager;

/// <summary>
/// A mock, non-existent <see cref="IFileSystemInfo{TFileInfo,TDirectoryInfo}"/>.
/// </summary>
public sealed class NonExistentMockFileSystemInfo : MockFileSystemInfo
{
	/// <summary>
	/// Creates a new instance of <see cref="NonExistentMockFileSystemInfo"/>.
	/// </summary>
	/// <param name="name">The entry's name in its <see cref="IFileSystemInfo.Parent"/>.</param>
	/// <param name="parent">The <see cref="IFileSystemInfo.Parent"/> of this entry.</param>
	public NonExistentMockFileSystemInfo(string name, MockDirectoryInfo? parent = null) : base(name, exists: false)
	{
		this.Parent = parent;
	}
}
