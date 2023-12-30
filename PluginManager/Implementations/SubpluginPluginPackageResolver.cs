using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

public sealed class SubpluginPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IPluginPackageResolver<TPluginManifest> BaseResolver { get; }
	private Func<IPluginPackage<TPluginManifest>, IEnumerable<IPluginPackageResolver<TPluginManifest>>> SubpluginResolverFactory { get; }

	public SubpluginPluginPackageResolver(
		IPluginPackageResolver<TPluginManifest> baseResolver,
		Func<IPluginPackage<TPluginManifest>, IEnumerable<IPluginPackageResolver<TPluginManifest>>> subpluginResolverFactory
	)
	{
		this.BaseResolver = baseResolver;
		this.SubpluginResolverFactory = subpluginResolverFactory;
	}

	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
	{
		foreach (var basePackageResult in this.BaseResolver.ResolvePluginPackages())
		{
			yield return basePackageResult;
			if (basePackageResult.TryPickT1(out _, out var basePackage))
				continue;
			foreach (var subpluginResolver in this.SubpluginResolverFactory(basePackage))
				foreach (var subpackage in subpluginResolver.ResolvePluginPackages())
					yield return subpackage;
		}
	}
}
