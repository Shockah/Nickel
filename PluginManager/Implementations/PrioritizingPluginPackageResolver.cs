using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Nanoray.PluginManager.Implementations;

/// <summary>
/// An <see cref="IPluginPackageResolver{TPluginManifest}"/> which skips plugins with a duplicate key if they are lower priority.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPriority">The priority comparable type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
public sealed class PrioritizingPluginPackageResolver<TPluginManifest, TPriority, TKey> : IPluginPackageResolver<TPluginManifest>
	where TKey : IEquatable<TKey>
	where TPriority : struct, INumber<TPriority>
{
	private readonly IPluginPackageResolver<PluginManifestWithPriority<TPluginManifest, TPriority>> Resolver;
	private readonly Func<IPluginPackage<PluginManifestWithPriority<TPluginManifest, TPriority>>, TKey> KeyFunction;

	/// <summary>
	/// Creates a new <see cref="PrioritizingPluginPackageResolver{TPluginManifest,TPriority,TKey}"/>.
	/// </summary>
	/// <param name="resolver">The underlying resolver.</param>
	/// <param name="keyFunction">A function mapping a plugin package to the key that needs to be distinct.</param>
	public PrioritizingPluginPackageResolver(IPluginPackageResolver<PluginManifestWithPriority<TPluginManifest, TPriority>> resolver, Func<IPluginPackage<PluginManifestWithPriority<TPluginManifest, TPriority>>, TKey> keyFunction)
	{
		this.Resolver = resolver;
		this.KeyFunction = keyFunction;
	}

	/// <inheritdoc/>
	public IEnumerable<PluginPackageResolveResult<TPluginManifest>> ResolvePluginPackages()
	{
		Dictionary<TKey, List<PluginPackageResolveResult<PluginManifestWithPriority<TPluginManifest, TPriority>>.Success>> keyToSuccesses = [];
		Dictionary<TKey, TPriority> keyToHighestPriority = [];
		List<PluginPackageResolveResult<TPluginManifest>> results = [];

		foreach (var resolveResult in this.Resolver.ResolvePluginPackages())
		{
			if (resolveResult.TryPickT1(out var error, out var success))
			{
				results.Add(error);
				continue;
			}
			
			var key = this.KeyFunction(success.Package);

			ref var successes = ref CollectionsMarshal.GetValueRefOrAddDefault(keyToSuccesses, key, out var successesExists);
			if (!successesExists)
				successes = [];
			successes!.Add(success);

			ref var highestPriority = ref CollectionsMarshal.GetValueRefOrAddDefault(keyToHighestPriority, key, out var highestPriorityExists);
			if (!highestPriorityExists || success.Package.Manifest.Priority > highestPriority)
				highestPriority = success.Package.Manifest.Priority;
		}

		foreach (var (key, highestPriority) in keyToHighestPriority)
		{
			if (!keyToSuccesses.TryGetValue(key, out var successes))
				continue;

			var highestPrioritySuccesses = successes
				.Where(s => s.Package.Manifest.Priority >= highestPriority)
				.ToList();
			if (highestPrioritySuccesses.Count <= 0)
				continue;
			
			var lowerPrioritySuccesses = successes
				.Where(s => s.Package.Manifest.Priority < highestPriority)
				.ToList();

			var extraWarnings = lowerPrioritySuccesses
				.Select(s => $"Ignored package {s.Package} due to lower priority than another one.")
				.ToList();
			
			results.AddRange(
				successes
					.Where(s => s.Package.Manifest.Priority >= highestPriority)
					.Select(s => new PluginPackageResolveResult<TPluginManifest>.Success
					{
						Package = new PluginPackage(s.Package),
						Warnings = [.. s.Warnings, .. extraWarnings]
					})
					.Select(s => (PluginPackageResolveResult<TPluginManifest>)s)
			);
		}

		return results;
	}

	private sealed class PluginPackage(IPluginPackage<PluginManifestWithPriority<TPluginManifest, TPriority>> package) : IPluginPackage<TPluginManifest>
	{
		public TPluginManifest Manifest
			=> package.Manifest.Manifest;

		public IDirectoryInfo PackageRoot
			=> package.PackageRoot;
		
		public void Dispose()
			=> package.Dispose();

		public override string ToString()
			=> $"PrioritizingPluginPackageResolver.PluginPackage {{ Package = {package}, Priority = {package.Manifest.Priority} }}";
	}
}
