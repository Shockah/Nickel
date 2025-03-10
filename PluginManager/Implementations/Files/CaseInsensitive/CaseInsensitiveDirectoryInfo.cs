using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager.CaseInsensitive;

/// <summary>
/// An <see cref="IDirectoryInfo{TFileInfo,TDirectoryInfo}"/> wrapper which makes any file operations case insensitive.
/// </summary>
public sealed class CaseInsensitiveDirectoryInfo(IDirectoryInfo wrapped) : CaseInsensitiveFileSystemInfo(wrapped), IDirectoryInfo<CaseInsensitiveFileInfo, CaseInsensitiveDirectoryInfo>, IDirectoryInfoWrapper
{
	/// <inheritdoc/>
	public new IDirectoryInfo Wrapped { get; } = wrapped;
	
	/// <inheritdoc/>
	public IEnumerable<IFileSystemInfo<CaseInsensitiveFileInfo, CaseInsensitiveDirectoryInfo>> Children
		=> this.Wrapped.Children.Select(c => c switch
		{
			IFileInfo file => new CaseInsensitiveFileInfo(file),
			IDirectoryInfo directory => new CaseInsensitiveDirectoryInfo(directory),
			_ => new CaseInsensitiveFileSystemInfo(c)
		});
	
	/// <inheritdoc/>
	public IFileSystemInfo<CaseInsensitiveFileInfo, CaseInsensitiveDirectoryInfo> GetRelative(string relativePath)
		=> new CaseInsensitiveFileSystemInfo(this.Wrapped.GetRelative(relativePath));

	/// <inheritdoc/>
	public CaseInsensitiveFileInfo GetRelativeFile(string relativePath)
		=> new(this.Wrapped.GetRelativeFile(relativePath));
	
	/// <inheritdoc/>
	public CaseInsensitiveDirectoryInfo GetRelativeDirectory(string relativePath)
		=> new(this.Wrapped.GetRelativeDirectory(relativePath));
}
