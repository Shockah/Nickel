using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using Nanoray.PluginManager.Implementations;
using Nickel.Common;
using Nickel.Framework.Implementations;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;

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
			loader: new AssemblyPluginLoader<IAssemblyModManifest, Mod, Mod>(
				requiredPluginDataProvider: p => new AssemblyPluginLoaderRequiredPluginData
				{
					UniqueName = p.Manifest.UniqueName,
					EntryPointAssembly = p.Manifest.EntryPointAssembly,
					EntryPointType = p.Manifest.EntryPointType
				},
				partAssembler: new SingleAssemblyPluginPartAssembler<IAssemblyModManifest, Mod>(),
				parameterInjector: assemblyPluginLoaderParameterInjector,
				assemblyEditor: extendableAssemblyDefinitionEditor
			),
			condition: package => package.Manifest.ModType == NickelConstants.AssemblyModType
		);

		var legacyAssemblyPluginLoader = new ConditionalPluginLoader<IAssemblyModManifest, Mod>(
			loader: new AssemblyPluginLoader<IAssemblyModManifest, ILegacyManifest, Mod>(
				requiredPluginDataProvider: p =>
					new AssemblyPluginLoaderRequiredPluginData
					{
						UniqueName = p.Manifest.UniqueName,
						EntryPointAssembly = p.Manifest.EntryPointAssembly,
						EntryPointType = p.Manifest.EntryPointType
					},
				partAssembler: new LegacyAssemblyPluginLoaderPartAssembler(
					helperProvider: this.ObtainModHelper,
					loggerProvider: this.ObtainLogger,
					cobaltCoreAssemblyProvider: () => this.CobaltCoreAssembly!,
					databaseProvider: () => this.LegacyDatabase!
				),
				parameterInjector: assemblyPluginLoaderParameterInjector,
				assemblyEditor: extendableAssemblyDefinitionEditor
			),
			condition: package => package.Manifest.ModType == NickelConstants.LegacyModType
				&& this.CobaltCoreAssembly is not null && this.LegacyDatabase is not null
				&& package.PackageRoot is IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>
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
			
			var displayName = GetModDisplayName(manifest);
			this.Logger.LogDebug("Loading mod {DisplayName}...", displayName);

			var failedRequiredDependencies = manifest.Dependencies
				.Where(d => d.IsRequired && this.FailedMods.Any(m => m.UniqueName == d.UniqueName))
				.ToList();
			if (failedRequiredDependencies.Count > 0)
			{
				this.FailedMods.Add(manifest);
				this.Logger.LogError(
					"Could not load {DisplayName}: Required dependencies failed to load: {Dependencies}",
					displayName,
					string.Join(", ", failedRequiredDependencies.Select(d => d.UniqueName))
				);
				continue;
			}

			if (!this.ExtendablePluginLoader.CanLoadPlugin(package))
			{
				this.FailedMods.Add(manifest);
				this.Logger.LogError(
					"Could not load {DisplayName}: no registered loader for this kind of mod.",
					displayName
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
						this.Logger.LogInformation("Loaded mod {DisplayName}.", displayName);
					},
					error =>
					{
						this.FailedMods.Add(manifest);
						this.Logger.LogError("Could not load {DisplayName}: {Error}", displayName, error.Value);
					}
				);
		}

		this.Logger.LogInformation("Loaded {Count} mods.", successfulMods.Count);
		this.EventManager.OnModLoadPhaseFinishedEvent.Raise(null, phase);
	}

	private static string GetModDisplayName(IModManifest manifest)
	{
		if (string.IsNullOrEmpty(manifest.DisplayName))
			return string.IsNullOrEmpty(manifest.Author)
				? manifest.UniqueName
				: $"{manifest.UniqueName} by {manifest.Author}";
		else
			return string.IsNullOrEmpty(manifest.Author)
				? $"{manifest.DisplayName} [{manifest.UniqueName}]"
				: $"{manifest.UniqueName} by {manifest.Author} [{manifest.UniqueName}]";
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
