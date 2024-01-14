using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using OneOf.Types;
using System;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;

namespace Nickel.Legacy;

public sealed class ModEntry : Mod
{
	public static string LegacyModType { get; } = $"{typeof(NickelConstants).Namespace!}.Legacy";

	private LegacyDatabase Database { get; }

	public ModEntry(
		IPluginPackage<IModManifest> package,
		IModHelper helper,
		ILogger logger,
		ExtendablePluginLoader<IModManifest, Mod> extendablePluginLoader,
		Func<IPluginPackage<IModManifest>, IModHelper> byPackageHelperProvider,
		Func<IModManifest, IModHelper> byManifestHelperProvider,
		Func<IModManifest, ILogger> loggerProvider,
		IAssemblyPluginLoaderLoadContextProvider<IAssemblyModManifest> loadContextProvider,
		IAssemblyPluginLoaderParameterInjector<IModManifest> assemblyPluginLoaderParameterInjector,
		IAssemblyEditor assemblyEditor
	)
	{
		this.Database = new(byManifestHelperProvider);
		helper.Events.OnModLoadPhaseFinished += this.OnModLoadPhaseFinished;
		helper.Events.OnLoadStringsForLocale += this.OnLoadStringsForLocale;

		var legacyAssemblyPluginLoader = new CallbackPluginLoader<IModManifest, Mod>(
			loader: new ConditionalPluginLoader<IModManifest, Mod>(
				loader: new SpecializedConvertingManifestPluginLoader<IAssemblyModManifest, IModManifest, Mod>(
					loader: new AssemblyPluginLoader<IAssemblyModManifest, ILegacyManifest, Mod>(
						requiredPluginDataProvider: p =>
							new AssemblyPluginLoaderRequiredPluginData
							{
								UniqueName = p.Manifest.UniqueName,
								EntryPointAssembly = p.Manifest.EntryPointAssembly,
								EntryPointType = p.Manifest.EntryPointType
							},
						loadContextProvider: loadContextProvider,
						partAssembler: new LegacyAssemblyPluginLoaderPartAssembler(
							helperProvider: byPackageHelperProvider,
							loggerProvider: loggerProvider,
							this.Database
						),
						parameterInjector: assemblyPluginLoaderParameterInjector,
						assemblyEditor: assemblyEditor
					),
					converter: m => m.AsAssemblyModManifest()
				),
				condition: package =>
				{
					if (package.Manifest.ModType != LegacyModType)
						return new No();
					return package.PackageRoot is IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>
						? new Yes()
						: new Error<string>($"Found a legacy mod, but it is not stored directly on disk (is it in a ZIP file?).");
				}
			),
			callback: (Mod mod) =>
			{
				if (mod is not LegacyModWrapper legacy)
					return;
				this.Database.LegacyManifests.AddRange(legacy.LegacyManifests);
			}
		);

		extendablePluginLoader.RegisterPluginLoader(legacyAssemblyPluginLoader);
	}

	[EventPriority(double.MaxValue)]
	private void OnModLoadPhaseFinished(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterDbInit)
			return;
		this.Database.AfterDbInit();
	}

	[EventPriority(double.MaxValue)]
	private void OnLoadStringsForLocale(object? _, LoadStringsForLocaleEventArgs e)
		=> this.Database.InjectLocalizations(e.Locale, e.Localizations);
}
