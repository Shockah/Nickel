using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;

namespace Nickel;

internal sealed class LegacyAssemblyPluginLoaderPartAssembler : IAssemblyPluginLoaderPartAssembler<IAssemblyModManifest, ILegacyManifest, Mod>
{
	private readonly Func<IPluginPackage<IModManifest>, IModHelper> HelperProvider;
	private readonly Func<IModManifest, ILogger> LoggerProvider;
	private readonly LegacyDatabase Database;

	public LegacyAssemblyPluginLoaderPartAssembler(
		Func<IPluginPackage<IModManifest>, IModHelper> helperProvider,
		Func<IModManifest, ILogger> loggerProvider,
		LegacyDatabase database
	)
	{
		this.HelperProvider = helperProvider;
		this.LoggerProvider = loggerProvider;
		this.Database = database;
	}

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
		var helper = this.HelperProvider(package);
		var logger = this.LoggerProvider(package.Manifest);
		var registry = new LegacyRegistry(package.Manifest, helper, logger, this.Database);
		return new LegacyModWrapper(package, parts, registry, helper, logger);
	}
}
