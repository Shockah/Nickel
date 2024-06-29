using System.Collections.Generic;

namespace Nanoray.PluginManager.Implementations;

/// <summary>
/// An <see cref="IPluginPackageResolver{TPluginManifest}"/> which resolves single plugin packages contained in another plugin package.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public sealed class InnerPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IPluginPackage<TPluginManifest> OuterPackage { get; }
	private TPluginManifest InnerManifest { get; }
	private bool DisposesOuterPackage { get; }

	/// <summary>
	/// Creates a new <see cref="InnerPluginPackageResolver{TPluginManifest}"/>.
	/// </summary>
	/// <param name="outerPackage">The outer plugin package.</param>
	/// <param name="innerManifest">The inner plugin manifest.</param>
	/// <param name="disposesOuterPackage">Whether the resolved package should dispose the outer package when it is itself disposed.</param>
	public InnerPluginPackageResolver(IPluginPackage<TPluginManifest> outerPackage, TPluginManifest innerManifest, bool disposesOuterPackage)
	{
		this.OuterPackage = outerPackage;
		this.InnerManifest = innerManifest;
		this.DisposesOuterPackage = disposesOuterPackage;
	}

	/// <inheritdoc/>
	public IEnumerable<PluginPackageResolveResult<TPluginManifest>> ResolvePluginPackages()
	{
		yield return new PluginPackageResolveResult<TPluginManifest>.Success
		{
			Package = new InnerPluginPackage<TPluginManifest>(this.OuterPackage, this.InnerManifest, this.DisposesOuterPackage),
			Warnings = []
		};
	}
}
