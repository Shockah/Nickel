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
		=> GetCaseFixed(this).AsFile!.OpenRead();
}
