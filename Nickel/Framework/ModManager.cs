using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using Nanoray.PluginManager.Implementations;
using Nickel.Common;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ILegacyModManifest = CobaltCoreModding.Definitions.ModManifests.IModManifest;

namespace Nickel;

internal sealed class ModManager
{
	private DirectoryInfo ModsDirectory { get; }
	private ILoggerFactory LoggerFactory { get; }
	private ILogger Logger { get; }
	internal ModEventManager EventManager { get; }

	internal IModManifest ModLoaderModManifest { get; private init; }
	internal ModLoadPhase CurrentModLoadPhase { get; private set; } = ModLoadPhase.BeforeGameAssembly;

	private Assembly? CobaltCoreAssembly { get; set; }
	internal LegacyDatabase? LegacyDatabase { get; private set; }
	internal ContentManager? ContentManager { get; private set; }

	private ExtendablePluginLoader<IModManifest, Mod> ExtendablePluginLoader { get; } = new();
	private List<IPluginPackage<IModManifest>> ResolvedMods { get; } = [];
	private List<IModManifest> FailedMods { get; } = [];
	private HashSet<IModManifest> OptionalSubmods { get; } = [];

	private Dictionary<string, ILogger> UniqueNameToLogger { get; } = [];
	private Dictionary<string, IModHelper> UniqueNameToHelper { get; } = [];
	private Dictionary<string, Mod> UniqueNameToInstance { get; } = [];
	private Dictionary<string, IPluginPackage<IModManifest>> UniqueNameToPackage { get; } = [];

	public ModManager(
		DirectoryInfo modsDirectory,
		ILoggerFactory loggerFactory,
		ILogger logger,
		ExtendableAssemblyDefinitionEditor extendableAssemblyDefinitionEditor
	)
	{
		this.ModsDirectory = modsDirectory;
		this.LoggerFactory = loggerFactory;
		this.Logger = logger;
		this.EventManager = new(this.ObtainLogger);

		this.ModLoaderModManifest = new ModManifest()
		{
			UniqueName = NickelConstants.Name,
			Version = NickelConstants.Version,
			RequiredApiVersion = NickelConstants.Version
		};

		var assemblyPluginLoaderParameterInjector = new CompoundAssemblyPluginLoaderParameterInjector<IModManifest>(
			new DelegateAssemblyPluginLoaderParameterInjector<IModManifest, ILogger>(
				package => this.ObtainLogger(package.Manifest)
			),
			new DelegateAssemblyPluginLoaderParameterInjector<IModManifest, IModHelper>(
				package => this.ObtainModHelper(package.Manifest)
			),
			new ValueAssemblyPluginLoaderParameterInjector<IModManifest, ExtendablePluginLoader<IModManifest, Mod>>(
				this.ExtendablePluginLoader
			),
			new ValueAssemblyPluginLoaderParameterInjector<IModManifest, ExtendableAssemblyDefinitionEditor>(
				extendableAssemblyDefinitionEditor
			)
		);

		var assemblyPluginLoader = new ConditionalPluginLoader<IAssemblyModManifest, Mod>(
			loader: new AssemblyPluginLoader<IAssemblyModManifest, Mod>(
				requiredPluginDataProvider: p => new AssemblyPluginLoader<IAssemblyModManifest, Mod>.RequiredPluginData
				{
					UniqueName = p.Manifest.UniqueName,
					EntryPointAssemblyFileName = p.Manifest.EntryPointAssemblyFileName
				},
				parameterInjector: assemblyPluginLoaderParameterInjector,
				assemblyEditor: extendableAssemblyDefinitionEditor
			),
			condition: package => package.Manifest.ModType == NickelConstants.AssemblyModType
		);

		var legacyAssemblyPluginLoader = new ConditionalPluginLoader<IAssemblyModManifest, Mod>(
			loader: new LegacyAssemblyPluginLoader(
				loader: new AssemblyPluginLoader<IAssemblyModManifest, ILegacyModManifest>(
					requiredPluginDataProvider: p =>
						new AssemblyPluginLoader<IAssemblyModManifest, ILegacyModManifest>.RequiredPluginData
						{
							UniqueName = p.Manifest.UniqueName,
							EntryPointAssemblyFileName = p.Manifest.EntryPointAssemblyFileName
						},
					parameterInjector: assemblyPluginLoaderParameterInjector,
					assemblyEditor: extendableAssemblyDefinitionEditor
				),
				helperProvider: this.ObtainModHelper,
				loggerProvider: this.ObtainLogger,
				cobaltCoreAssemblyProvider: () => this.CobaltCoreAssembly!,
				databaseProvider: () => this.LegacyDatabase!
			),
			condition: package => package.Manifest.ModType == NickelConstants.LegacyModType
				&& this.CobaltCoreAssembly is not null && this.LegacyDatabase is not null
		);

		this.ExtendablePluginLoader.RegisterPluginLoader(
			new SpecializedConvertingManifestPluginLoader<IAssemblyModManifest, IModManifest, Mod>(
				assemblyPluginLoader,
				m => m.AsAssemblyModManifest()
			)
		);
		this.ExtendablePluginLoader.RegisterPluginLoader(
			new SpecializedConvertingManifestPluginLoader<IAssemblyModManifest, IModManifest, Mod>(
				legacyAssemblyPluginLoader,
				m => m.AsAssemblyModManifest()
			)
		);

		DBPatches.OnLoadStringsForLocale.Subscribe(this.OnLoadStringsForLocale);
	}

	private void OnLoadStringsForLocale(object? sender, LoadStringsForLocaleEventArgs e)
		=> this.EventManager.OnLoadStringsForLocaleEvent.Raise(sender, e);

	private static ModLoadPhase GetModLoadPhaseForManifest(IModManifest manifest)
		=> (manifest as IAssemblyModManifest)?.LoadPhase ?? ModLoadPhase.AfterGameAssembly;

	public void ResolveMods()
	{
		this.Logger.LogInformation("Resolving mods...");

		var pluginDependencyResolver =
			new MultiPhasePluginDependencyResolver<IModManifest, SemanticVersion, ModLoadPhase>(
				new PluginDependencyResolver<IModManifest, SemanticVersion>(
					requiredManifestDataProvider: p =>
						new PluginDependencyResolver<IModManifest, SemanticVersion>.RequiredManifestData
						{
							UniqueName = p.UniqueName,
							Version = p.Version,
							Dependencies = p.Dependencies
								.Select(
									d => new PluginDependency<SemanticVersion>(d.UniqueName, d.Version, d.IsRequired)
								)
								.ToHashSet()
						}
				),
				GetModLoadPhaseForManifest,
				Enum.GetValues<ModLoadPhase>()
			);
		IPluginManifestLoader<IModManifest> pluginManifestLoader = new SpecializedPluginManifestLoader<ModManifest, IModManifest>(new JsonPluginManifestLoader<ModManifest>());

		IPluginPackageResolver<IModManifest> CreatePluginPackageResolver(IDirectoryInfo directory, bool allowModsInRoot)
			=> new SubpluginPluginPackageResolver<IModManifest>(
				baseResolver: new RecursiveDirectoryPluginPackageResolver<IModManifest>(
					directory: directory,
					manifestFileName: "nickel.json",
					ignoreDotNames: true,
					allowPluginsInRoot: allowModsInRoot,
					directoryResolverFactory: d => new DirectoryPluginPackageResolver<IModManifest>(d, "nickel.json", pluginManifestLoader, SingleFilePluginPackageResolverNoManifestResult.Empty),
					fileResolverFactory: f => f.Name.EndsWith(".zip")
						? new ZipPluginPackageResolver<IModManifest>(f, d => CreatePluginPackageResolver(d, allowModsInRoot: true))
						: null
				),
				subpluginResolverFactory: p =>
				{
					foreach (var optionalSubmod in p.Manifest.Submods.Where(submod => submod.IsOptional))
						this.OptionalSubmods.Add(optionalSubmod.Manifest);
					return p.Manifest.Submods.Select(
						submod => new InnerPluginPackageResolver<IModManifest>(p, submod.Manifest, disposesOuterPackage: false)
					);
				}
			);

		var pluginPackageResolver = new ValidatingPluginPackageResolver<IModManifest>(
			resolver: CreatePluginPackageResolver(new DirectoryInfoImpl(this.ModsDirectory), allowModsInRoot: false),
			validator: package =>
			{
				if (package.Manifest.RequiredApiVersion > NickelConstants.Version)
					return new Error<string>(
						$"Mod {package.Manifest.UniqueName} requires API version {package.Manifest.RequiredApiVersion}, but {NickelConstants.Name} is currently {NickelConstants.Version}."
					);
				return null;
			}
		);

		var toLoadResults = pluginPackageResolver.ResolvePluginPackages().ToList();
		foreach (var toLoadResult in toLoadResults)
			toLoadResult.Switch(
				package => { },
				error => this.Logger.LogError("{PackageResolvingError}", error.Value)
			);
		var toLoad = toLoadResults
			.Where(r => r.IsT0)
			.Select(r => r.AsT0)
			.ToList();

		this.Logger.LogInformation("Resolving mod load order...");

		var dependencyResolverResult = pluginDependencyResolver.ResolveDependencies(toLoad.Select(p => p.Manifest));
		foreach (var (unresolvableManifest, reason) in dependencyResolverResult.Unresolvable)
		{
			if (this.OptionalSubmods.Contains(unresolvableManifest)) continue;
			reason.Switch(
				missingDependencies => this.Logger.LogError(
					"Could not load {UniqueName}: missing dependencies: {Dependencies}",
					unresolvableManifest.UniqueName,
					string.Join(", ", missingDependencies.Dependencies.Select(d => d.UniqueName))
				),
				dependencyCycle => this.Logger.LogError(
					"Could not load {UniqueName}: dependency cycle: {Cycle}",
					unresolvableManifest.UniqueName,
					string.Join(
						" -> ",
						dependencyCycle.Cycle.Values.Append(dependencyCycle.Cycle.Values[0]).Select(m => m.UniqueName)
					)
				),
				unknown => this.Logger.LogError(
					"Could not load {UniqueName}: unknown reason.",
					unresolvableManifest.UniqueName
				)
			);
		}

		this.ResolvedMods.AddRange(
			dependencyResolverResult.LoadSteps
				.SelectMany(step => step)
				.Select(
					m => toLoad.FirstOrDefault(p => p.Manifest.UniqueName == m.UniqueName)
						?? throw new InvalidOperationException()
				)
		);
	}

	public void LoadMods(ModLoadPhase phase)
	{
		this.Logger.LogInformation("Loading {Phase} phase mods...", phase);
		this.CurrentModLoadPhase = phase;

		List<IModManifest> successfulMods = [];
		foreach (var package in this.ResolvedMods)
		{
			var manifest = package.Manifest;
			if (this.UniqueNameToInstance.ContainsKey(manifest.UniqueName))
				continue;
			if (this.FailedMods.Contains(manifest))
				continue;
			if (GetModLoadPhaseForManifest(manifest) != phase)
				continue;
			this.Logger.LogDebug("Loading mod {UniqueName}...", manifest.UniqueName);

			var failedRequiredDependencies = manifest.Dependencies
				.Where(d => d.IsRequired && this.FailedMods.Any(m => m.UniqueName == d.UniqueName))
				.ToList();
			if (failedRequiredDependencies.Count > 0)
			{
				this.FailedMods.Add(manifest);
				this.Logger.LogError(
					"Could not load {UniqueName}: Required dependencies failed to load: {Dependencies}",
					manifest.UniqueName,
					string.Join(", ", failedRequiredDependencies.Select(d => d.UniqueName))
				);
				continue;
			}

			if (!this.ExtendablePluginLoader.CanLoadPlugin(package))
			{
				this.FailedMods.Add(manifest);
				this.Logger.LogError(
					"Could not load {UniqueName}: no registered loader for this kind of mod.",
					manifest.UniqueName
				);
				continue;
			}

			this.ExtendablePluginLoader.LoadPlugin(package)
				.Switch(
					mod =>
					{
						this.UniqueNameToPackage[manifest.UniqueName] = package;
						this.UniqueNameToInstance[manifest.UniqueName] = mod;
						successfulMods.Add(manifest);
						this.Logger.LogInformation("Loaded mod {UniqueName}.", manifest.UniqueName);
					},
					error =>
					{
						this.FailedMods.Add(manifest);
						this.Logger.LogError("Could not load {UniqueName}: {Error}", manifest.UniqueName, error.Value);
					}
				);
		}

		this.Logger.LogInformation("Loaded {Count} mods.", successfulMods.Count);
		this.EventManager.OnModLoadPhaseFinishedEvent.Raise(null, phase);
	}

	internal void ContinueAfterLoadingGameAssembly(Assembly cobaltCoreGameAssembly)
	{
		this.CobaltCoreAssembly = cobaltCoreGameAssembly;
		this.ContentManager = new(() => this.CurrentModLoadPhase, this.ObtainLogger);
		this.LegacyDatabase = new(() => this.ContentManager);
	}

	private ILogger ObtainLogger(IModManifest manifest)
	{
		if (!this.UniqueNameToLogger.TryGetValue(manifest.UniqueName, out var logger))
		{
			logger = this.LoggerFactory.CreateLogger(manifest.UniqueName);
			this.UniqueNameToLogger[manifest.UniqueName] = logger;
		}

		return logger;
	}

	private IModHelper ObtainModHelper(IModManifest manifest)
	{
		if (!this.UniqueNameToHelper.TryGetValue(manifest.UniqueName, out var helper))
		{
			ModRegistry modRegistry = new(manifest, this.UniqueNameToInstance, this.UniqueNameToPackage);
			ModEvents modEvents = new(manifest, this.EventManager);
			ModSprites modSprites = new(manifest, () => this.ContentManager!.Sprites);
			ModDecks modDecks = new(manifest, () => this.ContentManager!.Decks);
			ModStatuses modStatuses = new(manifest, () => this.ContentManager!.Statuses);
			ModCards modCards = new(manifest, () => this.ContentManager!.Cards);
			ModArtifacts modArtifacts = new(manifest, () => this.ContentManager!.Artifacts);
			ModCharacters modCharacters = new(manifest, () => this.ContentManager!.Characters);
			ModShips modShips = new(
				manifest,
				() => this.ContentManager!.Ships,
				() => this.ContentManager!.Parts
			);
			ModContent modContent = new(
				modSprites,
				modDecks,
				modStatuses,
				modCards,
				modArtifacts,
				modCharacters,
				modShips
			);
			helper = new ModHelper(modRegistry, modEvents, modContent, () => this.CurrentModLoadPhase);
			this.UniqueNameToHelper[manifest.UniqueName] = helper;
		}

		return helper;
	}
}
