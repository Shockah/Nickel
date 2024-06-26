using System.IO;

namespace Nanoray.PluginManager;

/// <summary>
/// A mock <see cref="IFileInfo{TFileInfo,TDirectoryInfo}"/>.
/// </summary>
public sealed class MockFileInfo : MockFileSystemInfo, IFileInfo<MockFileInfo, MockDirectoryInfo>
{
	private byte[] Data { get; }
	
	/// <summary>
	/// Creates a new instance of <see cref="MockFileInfo"/>.
	/// </summary>
	/// <param name="name">The entry's name in its <see cref="IFileSystemInfo.Parent"/>.</param>
	/// <param name="data">The data contained in the file.</param>
	/// <param name="exists">Whether this file exists.</param>
	public MockFileInfo(string name, byte[]? data = null, bool exists = true) : base(name, exists)
	{
		this.Data = data ?? [];
	}

	/// <inheritdoc/>
	public Stream OpenRead()
		=> this.Exists ? new MemoryStream(this.Data) : throw new FileNotFoundException();
}
