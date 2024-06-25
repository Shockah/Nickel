using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nanoray.PluginManager;

public sealed class SingleAssemblyPluginPartAssembler<TPluginManifest, TPlugin> : IAssemblyPluginLoaderPartAssembler<TPluginManifest, TPlugin, TPlugin>
	where TPlugin : notnull
{
	public Error<string>? ValidatePluginParts(IPluginPackage<TPluginManifest> package, Assembly assembly, IReadOnlySet<Type> partTypes)
		=> partTypes.Count switch
		{
			<= 0 => new($"The assembly {assembly} does not include any {typeof(TPlugin)} subclasses."),
			> 1 => new($"The assembly {assembly} includes multiple {typeof(TPlugin)} subclasses: {string.Join(", ", partTypes.Select(t => t.FullName))}."),
			_ => null
		};

	public OneOf<TPlugin, Error<string>> AssemblePluginParts(IPluginPackage<TPluginManifest> package, Assembly assembly, IReadOnlySet<TPlugin> parts)
		=> parts.Count switch
		{
			<= 0 => new Error<string>($"The assembly {assembly} does not include any {typeof(TPlugin)} subclasses."),
			> 1 => new Error<string>($"The assembly {assembly} includes multiple {typeof(TPlugin)} subclasses: {string.Join(", ", parts.Select(p => p.GetType().FullName))}."),
			_ => parts.Single()
		};
}
