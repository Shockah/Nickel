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
	{
		if (partTypes.Count <= 0)
			return new($"The assembly {assembly} does not include any {typeof(TPlugin)} subclasses.");
		if (partTypes.Count > 1)
			return new($"The assembly {assembly} includes multiple {typeof(TPlugin)} subclasses: {string.Join(", ", partTypes.Select(t => t.FullName))}.");
		return null;
	}

	public OneOf<TPlugin, Error<string>> AssemblePluginParts(IPluginPackage<TPluginManifest> package, Assembly assembly, IReadOnlySet<TPlugin> parts)
	{
		if (parts.Count <= 0)
			return new Error<string>($"The assembly {assembly} does not include any {typeof(TPlugin)} subclasses.");
		if (parts.Count > 1)
			return new Error<string>($"The assembly {assembly} includes multiple {typeof(TPlugin)} subclasses: {string.Join(", ", parts.Select(p => p.GetType().FullName))}.");
		return parts.Single();
	}
}
