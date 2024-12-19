using System.IO;

namespace Nanoray.PluginManager.CaseInsensitive;

/// <summary>
/// An <see cref="IFileInfo{TFileInfo,TDirectoryInfo}"/> wrapper which makes any file operations case insensitive.
/// </summary>
public sealed class CaseInsensitiveFileInfo(IFileInfo wrapped) : CaseInsensitiveFileSystemInfo(wrapped), IFileInfo<CaseInsensitiveFileInfo, CaseInsensitiveDirectoryInfo>, IFileInfoWrapper
{
	/// <inheritdoc/>
	public new IFileInfo Wrapped { get; } = wrapped;

	/// <inheritdoc/>
	public Stream OpenRead()
	{
		if (GetCaseFixed(this).AsFile is not { } file)
			throw new FileNotFoundException("File not found", this.FullName);
		return file.OpenRead();
	}

	/// <inheritdoc/>
	public byte[] ReadAllBytes()
	{
		if (GetCaseFixed(this).AsFile is not { } file)
			throw new FileNotFoundException("File not found", this.FullName);
		return file.ReadAllBytes();
	}
}
