using System.Runtime.Loader;

namespace Nanoray.PluginManager;

/// <summary>
/// A type that provides an <see cref="AssemblyLoadContext"/> for the given plugin package.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public interface IAssemblyPluginLoaderLoadContextProvider<in TPluginManifest>
{
	/// <summary>
	/// Provides an <see cref="AssemblyLoadContext"/> for the given plugin package.
	/// </summary>
	/// <param name="package">The plugin package.</param>
	/// <returns>An <see cref="AssemblyLoadContext"/> for the given plugin package.</returns>
	AssemblyLoadContext GetLoadContext(IPluginPackage<TPluginManifest> package);
}
