using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IAssemblyPluginLoaderPartAssembler{TPluginManifest,TPluginPart,TPlugin}"/> that does no real assembling and only returns a singular plugin part as the assembled plugin.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPlugin">The plugin type.</typeparam>
public sealed class SingleAssemblyPluginPartAssembler<TPluginManifest, TPlugin> : IAssemblyPluginLoaderPartAssembler<TPluginManifest, TPlugin, TPlugin>
	where TPlugin : notnull
{
	/// <inheritdoc/>
	public Error<string>? ValidatePluginParts(IPluginPackage<TPluginManifest> package, Assembly assembly, IReadOnlySet<Type> partTypes)
		=> partTypes.Count switch
		{
			<= 0 => new($"The assembly {assembly} does not include any {typeof(TPlugin)} subclasses."),
			> 1 => new($"The assembly {assembly} includes multiple {typeof(TPlugin)} subclasses: {string.Join(", ", partTypes.Select(t => t.FullName))}."),
			_ => null
		};

	/// <inheritdoc/>
	public OneOf<TPlugin, Error<string>> AssemblePluginParts(IPluginPackage<TPluginManifest> package, Assembly assembly, IReadOnlySet<TPlugin> parts)
		=> parts.Count switch
		{
			<= 0 => new Error<string>($"The assembly {assembly} does not include any {typeof(TPlugin)} subclasses."),
			> 1 => new Error<string>($"The assembly {assembly} includes multiple {typeof(TPlugin)} subclasses: {string.Join(", ", parts.Select(p => p.GetType().FullName))}."),
			_ => parts.Single()
		};
}
