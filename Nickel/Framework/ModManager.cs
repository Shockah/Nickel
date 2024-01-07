using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using Nanoray.PluginManager.Implementations;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Nickel.Common;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;

namespace Nickel;

internal sealed class ModManager
{
	private DirectoryInfo ModsDirectory { get; }
	private ILoggerFactory LoggerFactory { get; }
	private ILogger Logger { get; }
	internal ModEventManager EventManager { get; }

	internal IModManifest ModLoaderModManifest { get; private init; }
	internal ModLoadPhase CurrentModLoadPhase { get; private set; } = ModLoadPhase.BeforeGameAssembly;
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

		var loadContextProvider = new AssemblyModLoadContextProvider(
			AssemblyLoadContext.GetLoadContext(this.GetType().Assembly) ?? AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default
		);

		var assemblyPluginLoaderParameterInjector = new ExtendableAssemblyPluginLoaderParameterInjector<IModManifest>();
		assemblyPluginLoaderParameterInjector.RegisterParameterInjector(
			new ValueAssemblyPluginLoaderParameterInjector<IModManifest, ExtendablePluginLoader<IModManifest, Mod>>(
				this.ExtendablePluginLoader
			)
		);
		assemblyPluginLoaderParameterInjector.RegisterParameterInjector(
			new ValueAssemblyPluginLoaderParameterInjector<IModManifest, ExtendableAssemblyPluginLoaderParameterInjector<IModManifest>>(
				assemblyPluginLoaderParameterInjector
			)
		);
		assemblyPluginLoaderParameterInjector.RegisterParameterInjector(
			new ValueAssemblyPluginLoaderParameterInjector<IModManifest, ExtendableAssemblyDefinitionEditor>(
				extendableAssemblyDefinitionEditor
			)
		);
		assemblyPluginLoaderParameterInjector.RegisterParameterInjector(
			new ValueAssemblyPluginLoaderParameterInjector<IModManifest, IAssemblyPluginLoaderLoadContextProvider<IAssemblyModManifest>>(
				loadContextProvider
			)
		);

		assemblyPluginLoaderParameterInjector.RegisterParameterInjector(
			new DelegateAssemblyPluginLoaderParameterInjector<IModManifest, ILogger>(
				package => this.ObtainLogger(package.Manifest)
			)
		);
		assemblyPluginLoaderParameterInjector.RegisterParameterInjector(
			new ValueAssemblyPluginLoaderParameterInjector<IModManifest, Func<IModManifest, ILogger>>(
				this.ObtainLogger
			)
		);
		assemblyPluginLoaderParameterInjector.RegisterParameterInjector(
			new DelegateAssemblyPluginLoaderParameterInjector<IModManifest, IModHelper>(
				package => this.ObtainModHelper(package.Manifest)
			)
		);
		assemblyPluginLoaderParameterInjector.RegisterParameterInjector(
			new ValueAssemblyPluginLoaderParameterInjector<IModManifest, Func<IModManifest, IModHelper>>(
				this.ObtainModHelper
			)
		);

		var assemblyPluginLoader = new ConditionalPluginLoader<IModManifest, Mod>(
			loader: new SpecializedConvertingManifestPluginLoader<IAssemblyModManifest, IModManifest, Mod>(
				loader: new AssemblyPluginLoader<IAssemblyModManifest, Mod, Mod>(
					requiredPluginDataProvider: p => new AssemblyPluginLoaderRequiredPluginData
					{
						UniqueName = p.Manifest.UniqueName,
						EntryPointAssembly = p.Manifest.EntryPointAssembly,
						EntryPointType = p.Manifest.EntryPointType
					},
					loadContextProvider: loadContextProvider,
					partAssembler: new SingleAssemblyPluginPartAssembler<IAssemblyModManifest, Mod>(),
					parameterInjector: assemblyPluginLoaderParameterInjector,
					assemblyEditor: extendableAssemblyDefinitionEditor
				),
				converter: m => m.AsAssemblyModManifest()
			),
			condition: package => package.Manifest.ModType == NickelConstants.AssemblyModType
				? new Yes() : new No()
		);

		this.ExtendablePluginLoader.RegisterPluginLoader(assemblyPluginLoader);

		DBPatches.OnLoadStringsForLocale.Subscribe(this.OnLoadStringsForLocale);
	}

	private void OnLoadStringsForLocale(object? sender, LoadStringsForLocaleEventArgs e)
		=> this.EventManager.OnLoadStringsForLocaleEvent.Raise(sender, e);

	public void ResolveMods()
	{
		this.Logger.LogInformation("Resolving mods...");

		var pluginManifestLoader = new SpecializedPluginManifestLoader<ModManifest, IModManifest>(new JsonPluginManifestLoader<ModManifest>());

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
			resolver: new DistinctPluginPackageResolver<IModManifest, string>(
				resolver: CreatePluginPackageResolver(new DirectoryInfoImpl(this.ModsDirectory), allowModsInRoot: false),
				keyFunction: package => package.Manifest.UniqueName
			),
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
				manifest => manifest.LoadPhase,
				Enum.GetValues<ModLoadPhase>()
			);

		var dependencyResolverResult = pluginDependencyResolver.ResolveDependencies(toLoad.Select(p => p.Manifest));
		foreach (var (unresolvableManifest, reason) in dependencyResolverResult.Unresolvable)
		{
			if (this.OptionalSubmods.Contains(unresolvableManifest))
				continue;
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
			if (manifest.LoadPhase != phase)
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

			var canLoadYesNoOrError = this.ExtendablePluginLoader.CanLoadPlugin(package);
			if (canLoadYesNoOrError.TryPickT2(out var error, out var canLoadYesOrNo))
			{
				this.FailedMods.Add(manifest);
				this.Logger.LogError("Could not load {DisplayName}: {Error}", displayName, error.Value);
				continue;
			}
			if (canLoadYesOrNo.IsT1)
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

	internal void ContinueAfterLoadingGameAssembly()
		=> this.ContentManager = new(() => this.CurrentModLoadPhase, this.ObtainLogger);

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
			helper = new ModHelper(
				new ModRegistry(manifest, this.UniqueNameToInstance, this.UniqueNameToPackage),
				new ModEvents(manifest, this.EventManager),
				new ModContent(
					new ModSprites(manifest, () => this.ContentManager!.Sprites),
					new ModDecks(manifest, () => this.ContentManager!.Decks),
					new ModStatuses(manifest, () => this.ContentManager!.Statuses),
					new ModCards(manifest, () => this.ContentManager!.Cards),
					new ModArtifacts(manifest, () => this.ContentManager!.Artifacts),
					new ModCharacters(manifest, () => this.ContentManager!.Characters),
					new ModShips(
						manifest,
						() => this.ContentManager!.Ships,
						() => this.ContentManager!.Parts
					)
				),
				new ModGameAccess(),
				() => this.CurrentModLoadPhase
			);
			this.UniqueNameToHelper[manifest.UniqueName] = helper;
		}

		return helper;
	}
}
