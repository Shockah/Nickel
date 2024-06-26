using OneOf;
using OneOf.Types;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// A type that resolves a collection of plugin packages.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public interface IPluginPackageResolver<TPluginManifest>
{
	/// <summary>
	/// Resolves and enumerates plugin packages.
	/// </summary>
	/// <returns>An enumerator over plugin packages and potential errors when resolving.</returns>
	IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages();
}
