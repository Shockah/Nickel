using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginPackageResolver{TPluginManifest}"/> which combines results from multiple resolvers.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <param name="resolvers">The resolvers to combine results from.</param>
public sealed class CompoundPluginPackageResolver<TPluginManifest>(
	IEnumerable<IPluginPackageResolver<TPluginManifest>> resolvers
) : IPluginPackageResolver<TPluginManifest>
{
	/// <inheritdoc />
	public IEnumerable<PluginPackageResolveResult<TPluginManifest>> ResolvePluginPackages()
		=> resolvers.SelectMany(resolver => resolver.ResolvePluginPackages());
}
