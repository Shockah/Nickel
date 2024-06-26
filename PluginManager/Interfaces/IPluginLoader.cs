using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager;

/// <summary>
/// A type that loads plugins from plugin packages.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPlugin">The plugin type.</typeparam>
public interface IPluginLoader<in TPluginManifest, TPlugin>
{
	/// <summary>
	/// Tests whether a plugin can be loaded from the given package.
	/// </summary>
	/// <param name="package">The plugin package.</param>
	/// <returns>Whether a plugin can be loaded from the given package.</returns>
	OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package);

	/// <summary>
	/// Loads a plugin from the given package.
	/// </summary>
	/// <param name="package">The plugin package.</param>
	/// <returns></returns>
	/// <remarks><see cref="CanLoadPlugin"/> should be called first, before attempting to call this method.</remarks>
	PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package);
}
