using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginPackageResolver{TPluginManifest}"/> which loads plugins as part of another plugin.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public sealed class SubpluginPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private readonly IPluginPackageResolver<TPluginManifest> BaseResolver;
	private readonly Func<IPluginPackage<TPluginManifest>, IEnumerable<IPluginPackageResolver<TPluginManifest>>> SubpluginResolverFactory;

	/// <summary>
	/// Creates a new <see cref="SubpluginPluginPackageResolver{TPluginManifest}"/>.
	/// </summary>
	/// <param name="baseResolver">The base plugin package resolver, providing root plugins.</param>
	/// <param name="subpluginResolverFactory">A function which builds additional plugin package resolvers for the given plugin package.</param>
	public SubpluginPluginPackageResolver(
		IPluginPackageResolver<TPluginManifest> baseResolver,
		Func<IPluginPackage<TPluginManifest>, IEnumerable<IPluginPackageResolver<TPluginManifest>>> subpluginResolverFactory
	)
	{
		this.BaseResolver = baseResolver;
		this.SubpluginResolverFactory = subpluginResolverFactory;
	}

	/// <inheritdoc/>
	public IEnumerable<PluginPackageResolveResult<TPluginManifest>> ResolvePluginPackages()
	{
		foreach (var baseResult in this.BaseResolver.ResolvePluginPackages())
		{
			yield return baseResult;
			if (baseResult.TryPickT1(out _, out var baseSuccess))
				continue;
			foreach (var subpluginResolver in this.SubpluginResolverFactory(baseSuccess.Package))
				foreach (var subpackage in subpluginResolver.ResolvePluginPackages())
					yield return subpackage;
		}
	}
}
