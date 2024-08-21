namespace Nanoray.PluginManager;

/// <summary>
/// Describes a wrapper <see cref="IDirectoryInfo"/>.
/// </summary>
public interface IDirectoryInfoWrapper : IDirectoryInfo, IFileSystemInfoWrapper
{
	/// <inheritdoc cref="IFileSystemInfoWrapper.Wrapped"/>
	new IDirectoryInfo Wrapped { get; }

	IFileSystemInfo IFileSystemInfoWrapper.Wrapped
		=> this.Wrapped;
}
