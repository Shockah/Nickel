using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager.Implementations;

/// <summary>
/// An <see cref="IPluginPackageResolver{TPluginManifest}"/> which disallows resolving of multiple plugins with the same key.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
public sealed class DistinctPluginPackageResolver<TPluginManifest, TKey> : IPluginPackageResolver<TPluginManifest>
	where TKey : IEquatable<TKey>
{
	private IPluginPackageResolver<TPluginManifest> Resolver { get; }
	private Func<IPluginPackage<TPluginManifest>, TKey> KeyFunction { get; }

	/// <summary>
	/// Creates a new <see cref="DistinctPluginPackageResolver{TPluginManifest,TKey}"/>.
	/// </summary>
	/// <param name="resolver">The underlying resolver.</param>
	/// <param name="keyFunction">A function mapping a plugin package to the key that needs to be distinct.</param>
	public DistinctPluginPackageResolver(IPluginPackageResolver<TPluginManifest> resolver, Func<IPluginPackage<TPluginManifest>, TKey> keyFunction)
	{
		this.Resolver = resolver;
		this.KeyFunction = keyFunction;
	}

	/// <inheritdoc/>
	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
	{
		List<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> results = [];
		Dictionary<TKey, List<IPluginPackage<TPluginManifest>>> keyToPackages = [];

		foreach (var packageOrError in this.Resolver.ResolvePluginPackages())
		{
			results.Add(packageOrError);
			if (packageOrError.TryPickT1(out _, out var package))
				continue;

			var key = this.KeyFunction(package);
			if (!keyToPackages.TryGetValue(key, out var packagesForKey))
			{
				packagesForKey = [];
				keyToPackages[key] = packagesForKey;
			}
			packagesForKey.Add(package);
		}

		foreach (var packagesForKey in keyToPackages.Values)
		{
			if (packagesForKey.Count < 2)
				continue;
			results.RemoveAll(result => result.TryPickT0(out var package, out _) && packagesForKey.Contains(package));
			results.Add(new Error<string>($"Found duplicate packages, none will be loaded:\n{string.Join("\n", packagesForKey.Select(p => $"\t{p}"))}"));
		}

		return results;
	}
}
