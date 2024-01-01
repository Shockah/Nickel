using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nanoray.PluginManager;

public interface IAssemblyPluginLoaderPartAssembler<in TPluginManifest, TPluginPart, TPlugin>
{
	Error<string>? ValidatePluginParts(IPluginPackage<TPluginManifest> package, Assembly assembly, IReadOnlySet<Type> partTypes);
	OneOf<TPlugin, Error<string>> AssemblePluginParts(IPluginPackage<TPluginManifest> package, Assembly assembly, IReadOnlySet<TPluginPart> parts);
}
