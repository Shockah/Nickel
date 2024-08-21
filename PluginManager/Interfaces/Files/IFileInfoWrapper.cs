namespace Nanoray.PluginManager;

/// <summary>
/// Describes a wrapper <see cref="IFileInfo"/>.
/// </summary>
public interface IFileInfoWrapper : IFileInfo, IFileSystemInfoWrapper
{
	/// <inheritdoc cref="IFileSystemInfoWrapper.Wrapped"/>
	new IFileInfo Wrapped { get; }

	IFileSystemInfo IFileSystemInfoWrapper.Wrapped
		=> this.Wrapped;
}
