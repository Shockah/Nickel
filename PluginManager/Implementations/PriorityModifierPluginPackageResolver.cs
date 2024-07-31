using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Nanoray.PluginManager.Implementations;

/// <summary>
/// An <see cref="IPluginPackageResolver{TPluginManifest}"/> that modifies the priority of plugin packages coming through it.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPriority">The priority comparable type.</typeparam>
public sealed class PriorityModifierPluginPackageResolver<TPluginManifest, TPriority> : IPluginPackageResolver<PluginManifestWithPriority<TPluginManifest, TPriority>>
	where TPriority : struct, INumber<TPriority>
{
	private readonly IPluginPackageResolver<PluginManifestWithPriority<TPluginManifest, TPriority>> Resolver;
	private readonly Func<TPluginManifest, TPriority, TPriority> PriorityFunction;

	/// <summary>
	/// Creates a new <see cref="PriorityModifierPluginPackageResolver{TPluginManifest,TPriority}"/>.
	/// </summary>
	/// <param name="resolver">The underlying resolver.</param>
	/// <param name="priorityFunction">The function that modifies the priority.</param>
	public PriorityModifierPluginPackageResolver(IPluginPackageResolver<PluginManifestWithPriority<TPluginManifest, TPriority>> resolver, Func<TPluginManifest, TPriority, TPriority> priorityFunction)
	{
		this.Resolver = resolver;
		this.PriorityFunction = priorityFunction;
	}

	/// <inheritdoc/>
	public IEnumerable<PluginPackageResolveResult<PluginManifestWithPriority<TPluginManifest, TPriority>>> ResolvePluginPackages()
		=> this.Resolver.ResolvePluginPackages().Select(resolveResult =>
		{
			if (resolveResult.TryPickT1(out var error, out var success))
				return error;
			return (PluginPackageResolveResult<PluginManifestWithPriority<TPluginManifest, TPriority>>)(success with
			{
				Package = new PluginPackage(success.Package, this.PriorityFunction(success.Package.Manifest.Manifest, success.Package.Manifest.Priority))
			});
		});
	
	private sealed class PluginPackage(IPluginPackage<PluginManifestWithPriority<TPluginManifest, TPriority>> package, TPriority priority) : IPluginPackage<PluginManifestWithPriority<TPluginManifest, TPriority>>
	{
		public PluginManifestWithPriority<TPluginManifest, TPriority> Manifest { get; } = package.Manifest with { Priority = priority };

		public IDirectoryInfo PackageRoot
			=> package.PackageRoot;
		
		public void Dispose()
			=> package.Dispose();

		public override string ToString()
			=> $"PriorityModifierPluginPackageResolver.PluginPackage {{ Package = {package}, Priority = {priority} }}";
	}
}
