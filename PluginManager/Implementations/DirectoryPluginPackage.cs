namespace Nanoray.PluginManager;

/// <summary>
/// A directory-based <see cref="IPluginPackage{TPluginManifest}"/>.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public sealed class DirectoryPluginPackage<TPluginManifest> : IPluginPackage<TPluginManifest>
{
	/// <inheritdoc/>
	public TPluginManifest Manifest { get; }
	
	/// <inheritdoc/>
	public IDirectoryInfo PackageRoot { get; }

	/// <summary>
	/// Creates a new <see cref="DirectoryPluginPackage{TPluginManifest}"/>.
	/// </summary>
	/// <param name="manifest">The plugin manifest.</param>
	/// <param name="directory">The directory containing the plugin.</param>
	public DirectoryPluginPackage(TPluginManifest manifest, IDirectoryInfo directory)
	{
		this.Manifest = manifest;
		this.PackageRoot = directory;
	}

	/// <inheritdoc/>
	public override string ToString()
		=> $"DirectoryPluginPackage {{ Manifest = {this.Manifest}, PackageRoot = {this.PackageRoot} }}";

	/// <inheritdoc/>
	public void Dispose()
	{
	}
}
