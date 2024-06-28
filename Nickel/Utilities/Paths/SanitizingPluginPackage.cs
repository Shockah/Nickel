using Nanoray.PluginManager;

namespace Nickel;

/// <summary>
/// An <see cref="IPluginPackage{TPluginManifest}"/> which sanitizes paths it prints.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <param name="package">An underlying plugin package.</param>
/// <seealso cref="PathUtilities.SanitizePath"/>
public sealed class SanitizingPluginPackage<TPluginManifest>(
	IPluginPackage<TPluginManifest> package
) : IPluginPackage<TPluginManifest>
{
	/// <inheritdoc/>
	public TPluginManifest Manifest
		=> package.Manifest;

	/// <inheritdoc/>
	public IDirectoryInfo PackageRoot
		=> package.PackageRoot;
	
	/// <inheritdoc/>
	public void Dispose()
		=> package.Dispose();
	
	/// <inheritdoc/>
	public override string ToString()
		=> $"SanitizingPluginPackage {{ PackageType = {package.GetType().Name}, Manifest = {this.Manifest}, PackageRoot = {PathUtilities.SanitizePath(this.PackageRoot.FullName)} }}";
}
