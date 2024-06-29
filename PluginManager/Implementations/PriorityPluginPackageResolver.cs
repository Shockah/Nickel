using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Nanoray.PluginManager.Implementations;

/// <summary>
/// An <see cref="IPluginPackageResolver{TPluginManifest}"/> that assigns a priority to plugin packages coming through it.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPriority">The priority comparable type.</typeparam>
public sealed class PriorityPluginPackageResolver<TPluginManifest, TPriority> : IPluginPackageResolver<PluginManifestWithPriority<TPluginManifest, TPriority>>
	where TPriority : struct, INumber<TPriority>
{
	private IPluginPackageResolver<TPluginManifest> Resolver { get; }
	private readonly TPriority Priority;

	/// <summary>
	/// Creates a new <see cref="PriorityPluginPackageResolver{TPluginManifest,TPriority}"/>.
	/// </summary>
	/// <param name="resolver">The underlying resolver.</param>
	/// <param name="priority">The priority of packages coming through this resolver.</param>
	public PriorityPluginPackageResolver(IPluginPackageResolver<TPluginManifest> resolver, TPriority priority)
	{
		this.Resolver = resolver;
		this.Priority = priority;
	}

	/// <inheritdoc/>
	public IEnumerable<PluginPackageResolveResult<PluginManifestWithPriority<TPluginManifest, TPriority>>> ResolvePluginPackages()
		=> this.Resolver.ResolvePluginPackages().Select(resolveResult =>
		{
			if (resolveResult.TryPickT1(out var error, out var success))
				return error;
			return (PluginPackageResolveResult<PluginManifestWithPriority<TPluginManifest, TPriority>>)new PluginPackageResolveResult<PluginManifestWithPriority<TPluginManifest, TPriority>>.Success
			{
				Package = new PluginPackage(success.Package, this.Priority),
				Warnings = success.Warnings
			};
		});
	
	private sealed class PluginPackage(IPluginPackage<TPluginManifest> package, TPriority priority) : IPluginPackage<PluginManifestWithPriority<TPluginManifest, TPriority>>
	{
		public PluginManifestWithPriority<TPluginManifest, TPriority> Manifest { get; } = new(package.Manifest, priority);

		public IDirectoryInfo PackageRoot
			=> package.PackageRoot;
		
		public void Dispose()
			=> package.Dispose();

		public override string ToString()
			=> $"PriorityPluginPackageResolver.PluginPackage {{ Package = {package}, Priority = {priority} }}";
	}
}
