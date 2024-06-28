using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
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
	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
	{
		foreach (var result in resolver.ResolvePluginPackages())
		{
			if (result.TryPickT1(out var error, out var package))
				yield return error;
			yield return new SanitizingPluginPackage<TPluginManifest>(package);
		}
	}
}
