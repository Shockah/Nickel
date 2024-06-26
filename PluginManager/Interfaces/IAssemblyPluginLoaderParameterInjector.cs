using System;

namespace Nanoray.PluginManager;

/// <summary>
/// A type that injects parameters to be used with constructors.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public interface IAssemblyPluginLoaderParameterInjector<in TPluginManifest>
{
	/// <summary>
	/// Attempts to inject a parameter value.
	/// </summary>
	/// <param name="package">The plugin package.</param>
	/// <param name="type">The type of value that needs to be injected.</param>
	/// <param name="toInject">The value that is being injected, if succeeded.</param>
	/// <returns>Whether this injector succeeded in injecting a value.</returns>
	bool TryToInjectParameter(IPluginPackage<TPluginManifest> package, Type type, out object? toInject);
}
