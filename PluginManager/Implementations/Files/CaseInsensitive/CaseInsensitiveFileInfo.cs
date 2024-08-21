using System.IO;

namespace Nanoray.PluginManager.CaseInsensitive;

public sealed class CaseInsensitiveFileInfo(IFileInfo wrapped) : CaseInsensitiveFileSystemInfo(wrapped), IFileInfo<CaseInsensitiveFileInfo, CaseInsensitiveDirectoryInfo>, IFileInfoWrapper
{
	/// <inheritdoc/>
	public new IFileInfo Wrapped { get; } = wrapped;
	
	/// <inheritdoc/>
	public Stream OpenRead()
		=> GetCaseFixed(this).AsFile!.OpenRead();
}
