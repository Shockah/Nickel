using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;

namespace Nickel.Legacy;

internal sealed class LegacyAssemblyPluginLoaderPartAssembler(
	IModHelper helper,
	LegacyDatabase database
) : IAssemblyPluginLoaderPartAssembler<IAssemblyModManifest, ILegacyManifest, Mod>
{
	public Error<string>? ValidatePluginParts(IPluginPackage<IAssemblyModManifest> _, Assembly assembly, IReadOnlySet<Type> partTypes)
	{
		if (partTypes.Count <= 0)
			return new($"The assembly {assembly} does not include any {typeof(ILegacyManifest)} subclasses.");
		return null;
	}

	public OneOf<Mod, Error<string>> AssemblePluginParts(IPluginPackage<IAssemblyModManifest> package, Assembly assembly, IReadOnlySet<ILegacyManifest> parts)
	{
		if (parts.Count <= 0)
			return new Error<string>($"The assembly {assembly} does not include any {typeof(ILegacyManifest)} subclasses.");
		var modHelper = helper.ModRegistry.GetModHelper(package.Manifest);
		var modLogger = helper.ModRegistry.GetLogger(package.Manifest);
		var registry = new LegacyRegistry(package.Manifest, modHelper, modLogger, database);
		return new LegacyModWrapper(package, parts, registry, modHelper, modLogger);
	}
}
