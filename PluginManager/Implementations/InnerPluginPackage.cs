namespace Nanoray.PluginManager;

public sealed class InnerPluginPackage<TPluginManifest> : IPluginPackage<TPluginManifest>
{
	public TPluginManifest Manifest { get; }
	public IDirectoryInfo PackageRoot { get; }

	private IPluginPackage<TPluginManifest> OuterPackage { get; }
	private bool DisposesOuterPackage { get; }

	public InnerPluginPackage(IPluginPackage<TPluginManifest> outerPackage, TPluginManifest manifest, bool disposesOuterPackage)
	{
		this.OuterPackage = outerPackage;
		this.Manifest = manifest;
		this.PackageRoot = outerPackage.PackageRoot;
		this.DisposesOuterPackage = disposesOuterPackage;
	}

	public override string ToString()
		=> $"InnerPluginPackage {{ Manifest = {this.Manifest}, PackageRoot = {this.PackageRoot}, OuterPackage = {this.OuterPackage} }}";

	public void Dispose()
	{
		if (this.DisposesOuterPackage)
			this.OuterPackage.Dispose();
	}
}
