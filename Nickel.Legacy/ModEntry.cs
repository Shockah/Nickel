using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel.Common;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;
using ILegacyModManifest = CobaltCoreModding.Definitions.ModManifests.IModManifest;

namespace Nickel.Legacy;

public sealed class ModEntry : Mod
{
	public static readonly string LegacyModType = $"{typeof(NickelConstants).Namespace!}.Legacy";

	private readonly LegacyDatabase Database;
	private readonly ILogger Logger;
	private readonly IModHelper Helper;
	private readonly IModManifest Manifest;

	public ModEntry(
		IPluginPackage<IModManifest> package,
		IModHelper helper,
		ILogger logger,
		ExtendablePluginLoader<IModManifest, Mod> extendablePluginLoader,
		Func<IPluginPackage<IModManifest>, IModHelper> byPackageHelperProvider,
		Func<IModManifest, IModHelper> byManifestHelperProvider,
		Func<IModManifest, ILogger> loggerProvider,
		IAssemblyPluginLoaderLoadContextProvider<IAssemblyModManifest> loadContextProvider,
		IAssemblyPluginLoaderParameterInjector<IModManifest> assemblyPluginLoaderParameterInjector
	)
	{
		this.Database = new(byManifestHelperProvider);
		this.Logger = logger;
		this.Helper = helper;
		this.Manifest = package.Manifest;
		
		var tempModDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), NickelConstants.Name, package.Manifest.UniqueName, "ExtractedModLibrary"));
		if (tempModDirectory.Exists)
			tempModDirectory.Delete(true);
		
		helper.Events.OnModLoadPhaseFinished += this.OnModLoadPhaseFinishedFirstPriority;
		helper.Events.OnModLoadPhaseFinished += this.OnModLoadPhaseFinishedLowPriority;
		helper.Events.OnLoadStringsForLocale += this.OnLoadStringsForLocale;

		this.Database.GlobalEventHub.MakeEvent(logger, LegacyEventHub.OnAfterGameAssemblyPhaseFinishedEvent, typeof(Func<ILegacyManifest, IPrelaunchContactPoint>));
		this.Database.GlobalEventHub.MakeEvent(logger, LegacyEventHub.OnAfterDbInitPhaseFinishedEvent, typeof(Func<ILegacyManifest, IPrelaunchContactPoint>));

		var legacyAssemblyPluginLoader = new CallbackPluginLoader<IModManifest, Mod>(
			loader: new ValidatingPluginLoader<IModManifest, Mod>(
				loader: new ConditionalPluginLoader<IModManifest, Mod>(
					loader: new CopyingPluginLoader<IModManifest, Mod>(
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
								parameterInjector: assemblyPluginLoaderParameterInjector
							),
							converter: m => m.AsAssemblyModManifest()
						),
						extractedDirectoryProvider: package =>
						{
							var packageRoot = package.PackageRoot;
							while (packageRoot is IDirectoryInfoWrapper packageRootWrapper)
								packageRoot = packageRootWrapper.Wrapped;
							
							if (packageRoot is IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>)
								return null; // no need to copy
							
							var rootExtractedPath = new DirectoryInfoImpl(new DirectoryInfo(Path.Combine(tempModDirectory.FullName, package.Manifest.UniqueName)));
							logger.LogInformation("Extracting mod {ModName} to {Path}.", package.Manifest.GetDisplayName(@long: false), PathUtilities.SanitizePath(rootExtractedPath.FullName));
							return rootExtractedPath;
						}
					),
					condition: package =>
					{
						if (package.Manifest.ModType != LegacyModType)
							return new No();
						return new Yes();
					}
				),
				validator: (package, mod) =>
				{
					List<string> warnings = [];
					if (mod is not LegacyModWrapper legacy)
						return new ValidatingPluginLoaderResult.Success { Warnings = warnings };
					if (!SemanticVersionParser.TryParseForAssembly(legacy.LegacyManifests.First().GetType().Assembly, out var assemblyVersion))
						return new ValidatingPluginLoaderResult.Success { Warnings = warnings };
					if (package.Manifest.Version.MajorVersion != assemblyVersion.MajorVersion || package.Manifest.Version.MinorVersion != assemblyVersion.MinorVersion || package.Manifest.Version.PatchVersion != assemblyVersion.PatchVersion)
						warnings.Add($"Found assembly version mismatch for mod {package.Manifest.GetDisplayName(@long: false)}: {assemblyVersion}");
					return new ValidatingPluginLoaderResult.Success { Warnings = warnings };
				}
			),
			callback: anyMod =>
			{
				if (anyMod is not LegacyModWrapper mod)
					return;
				
				foreach (var manifest in mod.LegacyManifests.OfType<ILegacyModManifest>())
					CatchIntoManifestLogger(manifest, nameof(ILegacyModManifest.BootMod), m => m.BootMod(mod.Registry));

				this.Database.LegacyMods.Add(mod);
				this.Database.LegacyManifests.AddRange(mod.LegacyManifests);
				foreach (var manifest in mod.LegacyManifests)
					this.Database.LegacyManifestToMod[manifest] = mod;
				
				foreach (var manifest in mod.LegacyManifests.OfType<INickelManifest>())
					CatchIntoManifestLogger(manifest, nameof(INickelManifest), m => m.OnNickelLoad(mod.Package, mod.Helper));
			}
		);

		extendablePluginLoader.RegisterPluginLoader(legacyAssemblyPluginLoader);
	}

	[EventPriority(double.MaxValue)]
	private void OnModLoadPhaseFinishedFirstPriority(object? _, ModLoadPhase phase)
	{
		if (phase == ModLoadPhase.AfterDbInit)
		{
			this.Database.AfterDbInit();
			this.GenerateManifestsForLegacyModsMissingOne(this.Helper.ModRegistry.ModsDirectory, isRoot: true);
		}

		switch (phase)
		{
			case ModLoadPhase.AfterGameAssembly:
				this.Database.GlobalEventHub.SignalEvent<Func<ILegacyManifest, IPrelaunchContactPoint>>(
					this.Logger,
					LegacyEventHub.OnAfterGameAssemblyPhaseFinishedEvent,
					m => this.Database.LegacyManifestToMod[m].Registry
				);
				break;
			case ModLoadPhase.AfterDbInit:
				this.Database.GlobalEventHub.SignalEvent<Func<ILegacyManifest, IPrelaunchContactPoint>>(
					this.Logger,
					LegacyEventHub.OnAfterDbInitPhaseFinishedEvent,
					m => this.Database.LegacyManifestToMod[m].Registry
				);
				break;
		}
	}

	[EventPriority(-100)]
	private void OnModLoadPhaseFinishedLowPriority(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		
		ForEachManifest<ISpriteManifest>(nameof(ISpriteManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<IGlossaryManifest>(nameof(IGlossaryManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<IDeckManifest>(nameof(IDeckManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<IStatusManifest>(nameof(IStatusManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<ICardManifest>(nameof(ICardManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<IArtifactManifest>(nameof(IArtifactManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<IAnimationManifest>(nameof(IAnimationManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<ICharacterManifest>(nameof(ICharacterManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<IPartTypeManifest>(nameof(IPartTypeManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<IShipPartManifest>(nameof(IShipPartManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<IShipManifest>(nameof(IShipManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<IStartershipManifest>(nameof(IStartershipManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<IStoryManifest>(nameof(IStoryManifest), (mod, manifest) => manifest.LoadManifest(mod.Registry));
		ForEachManifest<ICustomEventManifest>(nameof(ICustomEventManifest), (mod, manifest) => manifest.LoadManifest(mod.EventHub));
		ForEachManifest<IPrelaunchManifest>(nameof(IPrelaunchManifest), (mod, manifest) => manifest.FinalizePreperations(mod.Registry));

		void ForEachManifest<TLegacyManifest>(string tag, Action<LegacyModWrapper, TLegacyManifest> action) where TLegacyManifest : ILegacyManifest
		{
			foreach (var mod in this.Database.LegacyMods)
				foreach (var manifest in mod.LegacyManifests.OfType<TLegacyManifest>())
					CatchIntoManifestLogger(manifest, tag, m => action(mod, m));
		}
	}

	[EventPriority(double.MaxValue)]
	private void OnLoadStringsForLocale(object? _, LoadStringsForLocaleEventArgs e)
		=> this.Database.InjectLocalizations(e.Locale, e.Localizations);
	
	private static void CatchIntoManifestLogger<TLegacyManifest>(TLegacyManifest manifest, string tag, Action<TLegacyManifest> action)
		where TLegacyManifest : ILegacyManifest
	{
		try
		{
			action(manifest);
		}
		catch (Exception ex)
		{
			if (manifest.Logger is { } logger)
				logger.LogError("Mod failed in `{Tag}`: {Exception}", tag, ex);
			else
				throw;
		}
	}

	private void GenerateManifestsForLegacyModsMissingOne(DirectoryInfo directory, bool isRoot = false)
	{
		if (!isRoot)
		{
			var manifestFile = new FileInfo(Path.Combine(directory.FullName, NickelConstants.ManifestFileName));
			if (manifestFile.Exists)
				return;
		}

		var definiteModFile = new FileInfo(Path.Combine(directory.FullName, $"{directory.Name}.dll"));
		if (definiteModFile.Exists)
		{
			this.GenerateManifestForLegacyMod(directory, definiteModFile);
			return;
		}

		foreach (var dllFile in directory.EnumerateFiles("*.dll"))
			if (this.GenerateManifestForLegacyMod(directory, dllFile))
				return;

		foreach (var childDirectory in directory.GetDirectories())
			this.GenerateManifestsForLegacyModsMissingOne(childDirectory);
	}

	private bool GenerateManifestForLegacyMod(DirectoryInfo directory, FileInfo assemblyFileName)
	{
		Assembly assembly;
		try
		{
			var context = AssemblyLoadContext.GetLoadContext(this.GetType().Assembly) ?? AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default;
			assembly = context.LoadFromAssemblyPath(assemblyFileName.FullName);
		}
		catch
		{
			// not a managed DLL
			return false;
		}

		List<Type> manifestTypes;
		try
		{
			manifestTypes = assembly.GetTypes()
				.Where(t => t.IsAssignableTo(typeof(ILegacyManifest)))
				.ToList();
		}
		catch
		{
			// probably missing dependencies to load, we can't do anything about it
			// we don't even know if it's a legacy mod at this point, so just dropping out
			return false;
		}

		if (manifestTypes.Count == 0)
			return false;

		try
		{
			this.Logger.LogInformation("Generating a `{ManifestFileName}` file for a legacy mod that is missing one at `{Directory}`...", NickelConstants.ManifestFileName, PathUtilities.SanitizePath(directory.FullName));

			var manifests = manifestTypes
				.Select(t => (ILegacyManifest)Activator.CreateInstance(t)!)
				.ToList();
			var name = manifests.Select(m => m.Name).MinBy(m => m.Length) ?? assembly.GetName().Name ?? assembly.GetName().FullName;

			var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>() ?? throw new InvalidOperationException();
			var version = SemanticVersionParser.TryParse(attribute.InformationalVersion.Split("+")[0], out var parsedVersion)
				? parsedVersion : new SemanticVersion(1, 0, 0);

			var dependencies = manifests.SelectMany(m => m.Dependencies).ToList();
			var requiredDependencies = dependencies
				.Where(d => !d.IgnoreIfMissing)
				.DistinctBy(d => d.DependencyName);
			var optionalDependencies = dependencies
				.Where(d => d.IgnoreIfMissing)
				.Where(d => requiredDependencies.All(required => required.DependencyName != d.DependencyName))
				.DistinctBy(d => d.DependencyName);

			var manifest = new GeneratedLegacyModManifest
			{
				EntryPointAssembly = assemblyFileName.Name,
				UniqueName = name,
				RequiredApiVersion = this.Manifest.RequiredApiVersion,
				Version = version,
				Dependencies = new List<ModDependency>
				{
					new(this.Manifest.UniqueName, this.Manifest.Version, isRequired: true)
				}
					.Concat(requiredDependencies.Select(d => new ModDependency(d.DependencyName, isRequired: true)))
					.Concat(optionalDependencies.Select(d => new ModDependency(d.DependencyName, isRequired: false)))
					.ToHashSet()
			};

			var serializer = JsonSerializer.Create(new()
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
			});
			var manifestFile = new FileInfo(Path.Combine(directory.FullName, NickelConstants.ManifestFileName));
			using var stream = manifestFile.OpenWrite();
			using var streamWriter = new StreamWriter(stream);
			serializer.Serialize(streamWriter, manifest);

			this.Logger.LogWarning("Successfully generated a `{ManifestFileName}` file for a legacy mod at `{Directory}`. The mod will be loaded the next time you start {ModLoaderName}.", NickelConstants.ManifestFileName, PathUtilities.SanitizePath(directory.FullName), NickelConstants.Name);
			return true;
		}
		catch (Exception ex)
		{
			this.Logger.LogError("Could not generate a `{ManifestFileName}` file for legacy mod file `{LegacyModFileName}`: {Exception}", NickelConstants.ManifestFileName, assemblyFileName.Name, ex);
			// we failed, but this was definitely a legacy mod file, so we've "handled it" - don't recurse any deeper
			return true;
		}
	}
}
