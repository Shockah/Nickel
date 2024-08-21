namespace Nanoray.PluginManager;

/// <summary>
/// Describes a wrapper <see cref="IFileSystemInfo"/>.
/// </summary>
public interface IFileSystemInfoWrapper : IFileSystemInfo
{
	/// <summary>The underlying info.</summary>
	IFileSystemInfo Wrapped { get; }
}
