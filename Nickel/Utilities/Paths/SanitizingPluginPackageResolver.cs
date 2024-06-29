using Nanoray.PluginManager;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// An <see cref="IPluginPackageResolver{TPluginManifest}"/> which resolves a single plugin from a directory.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <param name="resolver">An underlying plugin package resolver.</param>
public sealed class SanitizingPluginPackageResolver<TPluginManifest>(
	IPluginPackageResolver<TPluginManifest> resolver
) : IPluginPackageResolver<TPluginManifest>
{
	/// <inheritdoc/>
	public IEnumerable<PluginPackageResolveResult<TPluginManifest>> ResolvePluginPackages()
	{
		foreach (var resolveResult in resolver.ResolvePluginPackages())
		{
			if (resolveResult.TryPickT1(out var error, out var success))
				yield return error;
			yield return success with { Package = new SanitizingPluginPackage<TPluginManifest>(success.Package) };
		}
	}
}
