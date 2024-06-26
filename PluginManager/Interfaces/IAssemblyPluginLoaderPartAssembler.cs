using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nanoray.PluginManager;

/// <summary>
/// A type which assembles a single plugin out of multiple smaller parts.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPluginPart">The plugin part type.</typeparam>
/// <typeparam name="TPlugin">The plugin type.</typeparam>
public interface IAssemblyPluginLoaderPartAssembler<in TPluginManifest, TPluginPart, TPlugin>
{
	/// <summary>
	/// Validates whether the provided plugin parts can be assembled into a single plugin.
	/// </summary>
	/// <param name="package">The plugin package.</param>
	/// <param name="assembly">The assembly containing the plugin parts.</param>
	/// <param name="partTypes">The types of plugin parts.</param>
	/// <returns>An error, if any.</returns>
	Error<string>? ValidatePluginParts(IPluginPackage<TPluginManifest> package, Assembly assembly, IReadOnlySet<Type> partTypes);
	
	/// <summary>
	/// Attempts to assemble a single plugin out of multiple smaller parts.
	/// </summary>
	/// <param name="package">The plugin package.</param>
	/// <param name="assembly">The assembly containing the plugin parts.</param>
	/// <param name="parts">The plugin parts.</param>
	/// <returns>An assembled plugin, or an error.</returns>
	OneOf<TPlugin, Error<string>> AssemblePluginParts(IPluginPackage<TPluginManifest> package, Assembly assembly, IReadOnlySet<TPluginPart> parts);
}
