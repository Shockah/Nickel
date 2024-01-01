using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;

namespace Nickel.Framework.Implementations;

internal sealed class LegacyAssemblyPluginLoaderPartAssembler : IAssemblyPluginLoaderPartAssembler<IAssemblyModManifest, ILegacyManifest, Mod>
{
	private Func<IModManifest, IModHelper> HelperProvider { get; }
	private Func<IModManifest, ILogger> LoggerProvider { get; }
	private Func<Assembly> CobaltCoreAssemblyProvider { get; }
	private Func<LegacyDatabase> DatabaseProvider { get; }

	public LegacyAssemblyPluginLoaderPartAssembler(
		Func<IModManifest, IModHelper> helperProvider,
		Func<IModManifest, ILogger> loggerProvider,
		Func<Assembly> cobaltCoreAssemblyProvider,
		Func<LegacyDatabase> databaseProvider
	)
	{
		this.HelperProvider = helperProvider;
		this.LoggerProvider = loggerProvider;
		this.CobaltCoreAssemblyProvider = cobaltCoreAssemblyProvider;
		this.DatabaseProvider = databaseProvider;
	}

	public Error<string>? ValidatePluginParts(IPluginPackage<IAssemblyModManifest> package, Assembly assembly, IReadOnlySet<Type> partTypes)
	{
		if (partTypes.Count <= 0)
			return new($"The assembly {assembly} does not include any {typeof(ILegacyManifest)} subclasses.");
		return null;
	}

	public OneOf<Mod, Error<string>> AssemblePluginParts(IPluginPackage<IAssemblyModManifest> package, Assembly assembly, IReadOnlySet<ILegacyManifest> parts)
	{
		if (parts.Count <= 0)
			return new Error<string>($"The assembly {assembly} does not include any {typeof(ILegacyManifest)} subclasses.");
		var helper = this.HelperProvider(package.Manifest);
		var logger = this.LoggerProvider(package.Manifest);
		LegacyRegistry registry = new(package.Manifest, helper, logger, this.CobaltCoreAssemblyProvider(), this.DatabaseProvider());
		return new LegacyModWrapper(parts, registry, package.PackageRoot, helper, logger);
	}
}
