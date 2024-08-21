namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginPackage{TPluginManifest}"/> for a plugin contained in another plugin package.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public sealed class InnerPluginPackage<TPluginManifest> : IPluginPackage<TPluginManifest>
{
	/// <inheritdoc/>
	public TPluginManifest Manifest { get; }
	
	/// <inheritdoc/>
	public IDirectoryInfo PackageRoot { get; }

	private readonly IPluginPackage<TPluginManifest> OuterPackage;
	private readonly bool DisposesOuterPackage;

	/// <summary>
	/// Creates a new <see cref="InnerPluginPackage{TPluginManifest}"/>.
	/// </summary>
	/// <param name="outerPackage">The outer plugin package.</param>
	/// <param name="manifest">The plugin manifest.</param>
	/// <param name="disposesOuterPackage">Whether this package should dispose the outer package when it is itself disposed.</param>
	public InnerPluginPackage(IPluginPackage<TPluginManifest> outerPackage, TPluginManifest manifest, bool disposesOuterPackage)
	{
		this.OuterPackage = outerPackage;
		this.Manifest = manifest;
		this.PackageRoot = outerPackage.PackageRoot;
		this.DisposesOuterPackage = disposesOuterPackage;
	}

	/// <inheritdoc/>
	public override string ToString()
		=> $"InnerPluginPackage {{ Manifest = {this.Manifest}, PackageRoot = {this.PackageRoot}, OuterPackage = {this.OuterPackage} }}";

	/// <inheritdoc/>
	public void Dispose()
	{
		if (this.DisposesOuterPackage)
			this.OuterPackage.Dispose();
	}
}
