using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Pintail;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using Nanoray.PluginManager.Implementations;
using Newtonsoft.Json.Serialization;
using Nickel.Common;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

namespace Nickel;

internal sealed class ModManager
{
	private readonly DirectoryInfo InternalModsDirectory;
	private readonly DirectoryInfo ModsDirectory;
	private readonly DirectoryInfo ModStorageDirectory;
	private readonly DirectoryInfo PrivateModStorageDirectory;
	private readonly ILoggerFactory LoggerFactory;
	internal readonly ILogger Logger;
	internal readonly ModEventManager EventManager;
	private readonly ModDataManager ModDataManager;
	private readonly ModStorageManager ModStorageManager;
	private readonly EnumCasePool EnumCasePool;
	internal readonly DelayedHarmonyManager DelayedHarmonyManager;

	internal readonly IPluginPackage<IModManifest> ModLoaderPackage;
	private ModLoadPhaseState CurrentModLoadPhase = new(ModLoadPhase.BeforeGameAssembly, IsDone: false);
	internal ContentManager? ContentManager { get; private set; }
	private IModManifest? VanillaModManifest;

	internal readonly List<IPluginPackage<IModManifest>> ResolvedMods = [];

	private readonly ExtendablePluginLoader<IModManifest, Mod> ExtendablePluginLoader = new();
	private readonly IProxyManager<string> ProxyManager;
	private readonly List<IModManifest> FailedMods = [];
	private readonly HashSet<IModManifest> OptionalSubmods = [];
	private bool DidLogHarmonyPatchesOnce;

	private readonly Dictionary<string, ILogger> UniqueNameToLogger = [];
	private readonly Dictionary<string, IModHelper> UniqueNameToHelper = [];
	private readonly Dictionary<string, Mod> UniqueNameToInstance = [];
	private readonly Dictionary<string, IPluginPackage<IModManifest>> UniqueNameToPackage = [];

	public ModManager(
		DirectoryInfo internalModsDirectory,
		DirectoryInfo modsDirectory,
		DirectoryInfo modStorageDirectory,
		DirectoryInfo privateModStorageDirectory,
		ILoggerFactory loggerFactory,
		ILogger logger,
		ExtendableAssemblyDefinitionEditor extendableAssemblyDefinitionEditor
	)
	{
		this.InternalModsDirectory = internalModsDirectory;
		this.ModsDirectory = modsDirectory;
		this.ModStorageDirectory = modStorageDirectory;
		this.PrivateModStorageDirectory = privateModStorageDirectory;
		this.LoggerFactory = loggerFactory;
		this.Logger = logger;

		this.ModLoaderPackage = new FakePluginPackage(
			manifest: new ModManifest
			{
				UniqueName = NickelConstants.Name,
				Version = NickelConstants.Version,
				DisplayName = NickelConstants.Name,
				Author = NickelConstants.Name,
				RequiredApiVersion = NickelConstants.Version
			},
			packageRoot: new DirectoryInfoImpl(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory))
		);

		this.EventManager = new(
			() => this.CurrentModLoadPhase,
			this.ObtainLogger,
			this.ModLoaderPackage.Manifest
		);
		this.ModDataManager = new();
		this.ModStorageManager = new();
		this.EnumCasePool = new();
		this.DelayedHarmonyManager = new();

		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"{this.GetType().Namespace}.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
		var moduleBuilder = assemblyBuilder.DefineDynamicModule($"{this.GetType().Namespace}.Proxies");
		this.ProxyManager = new ProxyManager<string>(moduleBuilder, new()
		{
			ProxyPrepareBehavior = ProxyManagerProxyPrepareBehavior.Eager,
			ProxyObjectInterfaceMarking = ProxyObjectInterfaceMarking.MarkerWithProperty,
			AccessLevelChecking = AccessLevelChecking.DisabledButOnlyAllowPublicMembers,
		});

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
				this.ObtainModHelper
			)
		);
		assemblyPluginLoaderParameterInjector.RegisterParameterInjector(
			new ValueAssemblyPluginLoaderParameterInjector<IModManifest, Func<IModManifest, IModHelper>>(
				this.ObtainModHelper
			)
		);

		var assemblyPluginLoader = new ValidatingPluginLoader<IModManifest, Mod>(
			loader: new ConditionalPluginLoader<IModManifest, Mod>(
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
				condition: package => package.Manifest.ModType == NickelConstants.ModType || package.Manifest.ModType == NickelConstants.DeprecatedModType
					? new Yes() : new No()
			),
			validator: (package, mod) =>
			{
				List<string> warnings = [];
				if (!SemanticVersionParser.TryParseForAssembly(mod.GetType().Assembly, out var assemblyVersion))
					return new ValidatingPluginLoaderResult.Success { Warnings = warnings };
				if (package.Manifest.ModType == NickelConstants.DeprecatedModType)
					warnings.Add($"Mod {package.Manifest.GetDisplayName(@long: false)} uses a deprecated ModType `{NickelConstants.DeprecatedModType}` - switch to `{NickelConstants.ModType}` instead.");
				if (package.Manifest.Version.MajorVersion != assemblyVersion.MajorVersion || package.Manifest.Version.MinorVersion != assemblyVersion.MinorVersion || package.Manifest.Version.PatchVersion != assemblyVersion.PatchVersion)
					warnings.Add($"Found assembly version mismatch for mod {package.Manifest.GetDisplayName(@long: false)}: {assemblyVersion}");
				return new ValidatingPluginLoaderResult.Success { Warnings = warnings };
			}
		);

		this.ExtendablePluginLoader.RegisterPluginLoader(assemblyPluginLoader);

		DBPatches.OnLoadStringsForLocale += this.OnLoadStringsForLocale;
	}

	private void OnLoadStringsForLocale(object? sender, LoadStringsForLocaleEventArgs e)
		=> this.EventManager.OnLoadStringsForLocaleEvent.Raise(sender, e);

	public void ResolveMods()
	{
		this.Logger.LogInformation("Resolving mods...");

		var pluginManifestLoader = new SpecializedPluginManifestLoader<ModManifest, IModManifest>(new JsonPluginManifestLoader<ModManifest>());

		var pluginPackageResolver = new ValidatingPluginPackageResolver<IModManifest>(
			resolver: new DistinctPluginPackageResolver<IModManifest, string>(
				resolver: new SubpluginPluginPackageResolver<IModManifest>(
					baseResolver: new PrioritizingPluginPackageResolver<IModManifest, double, string>(
						resolver: new CompoundPluginPackageResolver<PluginManifestWithPriority<IModManifest, double>>([
							new PriorityModifierPluginPackageResolver<IModManifest, double>(
								CreateDirectoryPluginPackageResolver(new DirectoryInfoImpl(this.InternalModsDirectory), false, 1.0, 0.75),
								(_, priority) => priority * 2
							),
							CreateDirectoryPluginPackageResolver(new DirectoryInfoImpl(this.ModsDirectory), false, 1.0, 0.75)
						]),
						keyFunction: p => p.Manifest.Manifest.UniqueName
					),
					subpluginResolverFactory: p =>
					{
						foreach (var optionalSubmod in p.Manifest.Submods.Where(submod => submod.IsOptional))
							this.OptionalSubmods.Add(optionalSubmod.Manifest);
						return p.Manifest.Submods.Select(
							submod => new InnerPluginPackageResolver<IModManifest>(p, submod.Manifest, disposesOuterPackage: false)
						);
					}
				),
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
				success =>
				{
					foreach (var warning in success.Warnings)
						this.Logger.LogInformation("{PackageResolvingWarning}", warning);
				},
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

		var dependencyResolverResult = pluginDependencyResolver.ResolveDependencies(
			toLoad
				.Select(p => p.Package.Manifest)
				.OrderBy(m => m.UniqueName)
		);
		foreach (var (unresolvableManifest, reason) in dependencyResolverResult.Unresolvable)
		{
			if (this.OptionalSubmods.Contains(unresolvableManifest))
				continue;
			reason.Switch(
				missingDependencies =>
				{
					if (missingDependencies.Missing.Count > 0)
					{
						this.Logger.LogError(
							"Could not load {UniqueName}: missing dependencies: {Dependencies}",
							unresolvableManifest.UniqueName,
							string.Join(", ", missingDependencies.Missing.Select(d => d.UniqueName))
						);
					}
					else if (missingDependencies.Misversioned.Count > 0)
					{
						this.Logger.LogError(
							"Could not load {UniqueName}:\n{Dependencies}",
							unresolvableManifest.UniqueName,
							string.Join(
								"\n",
								missingDependencies.Misversioned
									.Where(d => d.Version is not null) // this should always be true, but just in case
									.Select(d => $"\trequires {d.UniqueName} at version {d.Version!} or higher")
							)
						);
					}
					else
					{
						this.Logger.LogError(
							"Could not load {UniqueName}: unknown reason.",
							unresolvableManifest.UniqueName
						);
					}
				},
				dependencyCycle => this.Logger.LogError(
					"Could not load {UniqueName}: dependency cycle: {Cycle}",
					unresolvableManifest.UniqueName,
					string.Join(
						" -> ",
						dependencyCycle.Cycle.Values.Append(dependencyCycle.Cycle.Values[0]).Select(m => m.UniqueName)
					)
				),
				_ => this.Logger.LogError(
					"Could not load {UniqueName}: unknown reason.",
					unresolvableManifest.UniqueName
				)
			);
		}

		this.ResolvedMods.AddRange(
			dependencyResolverResult.LoadSteps
				.SelectMany(step => step)
				.Select(
					m => toLoad.FirstOrNull(p => p.Package.Manifest.UniqueName == m.UniqueName)?.Package
						?? throw new InvalidOperationException()
				)
		);
		
		IPluginPackageResolver<PluginManifestWithPriority<IModManifest, double>> CreateDirectoryPluginPackageResolver(IDirectoryInfo directory, bool allowModsInRoot, double extractedPriority, double compressedPriority)
			=> new RecursiveDirectoryPluginPackageResolver<PluginManifestWithPriority<IModManifest, double>>(
				directory: directory,
				manifestFileName: NickelConstants.ManifestFileName,
				ignoreDotNames: true,
				allowPluginsInRoot: allowModsInRoot,
				directoryResolverFactory: d => new PriorityPluginPackageResolver<IModManifest, double>(
					resolver: new SanitizingPluginPackageResolver<IModManifest>(
						new DirectoryPluginPackageResolver<IModManifest>(d, NickelConstants.ManifestFileName, pluginManifestLoader, SingleFilePluginPackageResolverNoManifestResult.Empty)
					),
					priority: extractedPriority
				),
				fileResolverFactory: f => f.Name.EndsWith(".zip")
					? new PriorityModifierPluginPackageResolver<IModManifest, double>(
						new ZipPluginPackageResolver<PluginManifestWithPriority<IModManifest, double>>(
							f,
							d => CreateDirectoryPluginPackageResolver(d, true, extractedPriority, compressedPriority)
						),
						(_, priority) => priority * compressedPriority
					) : null
			);
	}

	public void LoadMods(ModLoadPhase phase)
	{
		this.Logger.LogInformation("Loading {Phase} phase mods...", phase);
		this.CurrentModLoadPhase = new(phase, IsDone: false);

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

			var displayName = manifest.GetDisplayName(@long: false);
			this.Logger.LogDebug("Loading mod {DisplayName} from {Package}...", displayName, package.PackageRoot);

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

			try
			{
				this.ExtendablePluginLoader.LoadPlugin(package)
					.Switch(
						success =>
						{
							foreach (var warning in success.Warnings)
								this.Logger.LogWarning("{Warning}", warning);

							this.UniqueNameToPackage[manifest.UniqueName] = package;
							this.UniqueNameToInstance[manifest.UniqueName] = success.Plugin;
							successfulMods.Add(manifest);
							this.Logger.LogInformation("Loaded mod {DisplayName}", manifest.GetDisplayName(@long: true));
						},
						error =>
						{
							this.FailedMods.Add(manifest);
							this.Logger.LogError("Could not load {DisplayName}: {Error}", displayName, error.Value);
						}
					);
			}
			catch (Exception ex)
			{
				this.FailedMods.Add(manifest);
				this.Logger.LogError("Could not load {DisplayName}: {ex}", displayName, ex);
			}
		}

		this.CurrentModLoadPhase = new(this.CurrentModLoadPhase.Phase, IsDone: true);
		this.Logger.LogInformation("Loaded {Count} mods.", successfulMods.Count);
		this.EventManager.OnModLoadPhaseFinishedEvent.Raise(null, phase);

		if (phase == ModLoadPhase.AfterDbInit)
		{
			this.DelayedHarmonyManager.ApplyDelayedPatches();
			this.LogHarmonyPatchesOnce();
		}
	}

	public void LogHarmonyPatchesOnce()
	{
		if (this.DidLogHarmonyPatchesOnce)
			return;
		this.DidLogHarmonyPatchesOnce = true;

		var allPatchedMethods = Harmony.GetAllPatchedMethods().ToList();
		if (allPatchedMethods.Count == 0)
			return;

		var allMethodPatchInfoString = string.Join(
			"\n",
			allPatchedMethods.OrderBy(m => $"{m.DeclaringType?.FullName ?? m.DeclaringType?.Name}::{m.Name}").Select(m =>
			{
				var patchInfo = Harmony.GetPatchInfo(m);
				var patchInfoString = string.Join(
					"\n",
					patchInfo.Owners.OrderBy(owner => owner).Select(owner =>
					{
						var patchTypeStrings = new List<string>();
						if (patchInfo.Prefixes.Any(p => p.owner == owner && p.PatchMethod.ReturnType == typeof(bool)))
							patchTypeStrings.Add("skipping prefix");
						if (patchInfo.Prefixes.Any(p => p.owner == owner && p.PatchMethod.ReturnType != typeof(bool)))
							patchTypeStrings.Add("passthrough prefix");
						if (patchInfo.Postfixes.Any(p => p.owner == owner))
							patchTypeStrings.Add("postfix");
						if (patchInfo.Finalizers.Any(p => p.owner == owner))
							patchTypeStrings.Add("finalizer");
						if (patchInfo.Transpilers.Any(p => p.owner == owner))
							patchTypeStrings.Add("transpiler");
						return $"\t\t{owner} ({string.Join(", ", patchTypeStrings)})";
					})
				);
				return $"\t{m.DeclaringType?.FullName ?? m.DeclaringType?.Name}::{m.Name}\n{patchInfoString}";
			})
		);
		this.Logger.LogDebug("Methods patched with Harmony:\n{Methods}", allMethodPatchInfoString);
	}

	internal void ContinueAfterLoadingGameAssembly(SemanticVersion gameVersion)
	{
		this.VanillaModManifest = new ModManifest()
		{
			UniqueName = "CobaltCore",
			Version = gameVersion,
			DisplayName = "Cobalt Core",
			Author = "Rocket Rat Games",
			RequiredApiVersion = NickelConstants.Version
		};

		this.ContentManager = ContentManager.Create(() => this.CurrentModLoadPhase, this.ObtainLogger, this.EnumCasePool, this.VanillaModManifest, this.ModLoaderPackage.Manifest, this.ModDataManager);
		this.PrepareJsonSerialization();
	}

	private void PrepareJsonSerialization()
	{
		var proxyContractResolver = new ProxyContractResolver<string>(this.ProxyManager);

		JSONSettings.indented.Converters.Add(proxyContractResolver);
		JSONSettings.indented.ContractResolver = new ConditionalWeakTableExtensionDataContractResolver(
			new ModificatingContractResolver(
				contractModificator: this.ModifyJsonContract,
				wrapped: JSONSettings.indented.ContractResolver
			),
			this.Logger,
			ModDataManager.ModDataJsonKey,
			this.ModDataManager.ModDataStorage
		);

		JSONSettings.serializer.Converters.Add(proxyContractResolver);
		JSONSettings.serializer.ContractResolver = new ConditionalWeakTableExtensionDataContractResolver(
			new ModificatingContractResolver(
				contractModificator: this.ModifyJsonContract,
				wrapped: JSONSettings.serializer.ContractResolver
			),
			this.Logger,
			ModDataManager.ModDataJsonKey,
			this.ModDataManager.ModDataStorage
		);
	}

	private void ModifyJsonContract(Type type, JsonContract contract)
	{
		if (type.IsAssignableTo(typeof(IProxyObject.IWithProxyTargetInstanceProperty)))
		{
			contract.Converter = new ProxyContractResolver<string>(this.ProxyManager);
			return;
		}
		this.ContentManager!.ModifyJsonContract(type, contract);
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
		if (this.UniqueNameToHelper.TryGetValue(manifest.UniqueName, out var helper))
			return helper;
		if (!this.UniqueNameToPackage.TryGetValue(manifest.UniqueName, out var package))
			throw new InvalidOperationException();
		return this.ObtainModHelper(package);
	}

	internal IModHelper ObtainModHelper(IPluginPackage<IModManifest> package)
	{
		if (!this.UniqueNameToHelper.TryGetValue(package.Manifest.UniqueName, out var helper))
		{
			var modEvents = new ModEvents(package.Manifest, this.EventManager);
			
			helper = new ModHelper(
				new ModRegistry(
					package.Manifest,
					() => this.VanillaModManifest,
					() => this.ModLoaderPackage.Manifest,
					this.ModsDirectory,
					this.UniqueNameToInstance,
					this.UniqueNameToPackage,
					this.ResolvedMods,
					this.ProxyManager,
					() => this.CurrentModLoadPhase,
					modEvents
				),
				modEvents,
				() => new ModContent(
					new ModSprites(package, () => this.ContentManager!.Sprites),
					new ModDecks(package.Manifest, () => this.ContentManager!.Decks),
					new ModStatuses(package.Manifest, () => this.ContentManager!.Statuses),
					new ModCards(package.Manifest, () => this.ContentManager!.Cards, () => this.ContentManager!.CardTraits),
					new ModArtifacts(package.Manifest, () => this.ContentManager!.Artifacts),
					new ModCharacters(package.Manifest, () => this.ContentManager!.Characters),
					new ModShips(package.Manifest, () => this.ContentManager!.Ships, () => this.ContentManager!.Parts),
					new ModEnemies(package.Manifest, () => this.ContentManager!.Enemies)
				),
				new ModData(package.Manifest, this.ModDataManager),
				new ModStorage(
					package.Manifest,
					() => this.ObtainLogger(package.Manifest),
					new DirectoryInfoImpl(this.ModStorageDirectory),
					new DirectoryInfoImpl(this.PrivateModStorageDirectory),
					this.ModStorageManager
				),
				new ModUtilities(
					this.EnumCasePool,
					this.ProxyManager,
					this.DelayedHarmonyManager,
					new Harmony(package.Manifest.UniqueName)
				),
				() => this.CurrentModLoadPhase
			);
			this.UniqueNameToHelper[package.Manifest.UniqueName] = helper;
		}

		return helper;
	}

	private sealed class FakePluginPackage(IModManifest manifest, IDirectoryInfo packageRoot) : IPluginPackage<IModManifest>
	{
		public IModManifest Manifest { get; } = manifest;
		public IDirectoryInfo PackageRoot { get; } = packageRoot;
		
		public void Dispose()
		{
		}
	}
}
