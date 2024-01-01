namespace Nanoray.PluginManager;

public sealed class DirectoryPluginPackage<TPluginManifest> : IPluginPackage<TPluginManifest>
{
	public TPluginManifest Manifest { get; }
	public IDirectoryInfo PackageRoot { get; }

	public DirectoryPluginPackage(TPluginManifest manifest, IDirectoryInfo directory)
	{
		this.Manifest = manifest;
		this.PackageRoot = directory;
	}

	public override string ToString()
		=> $"DirectoryPluginPackage {{ Manifest = {this.Manifest}, PackageRoot = {this.PackageRoot} }}";

	public void Dispose()
	{
	}
}
