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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
	internal readonly IModDataHandler ModDataHandler;
	private readonly ModStorageManager ModStorageManager;
	private readonly EnumCasePool EnumCasePool;
	private readonly DelayedHarmonyManager DelayedHarmonyManager;
	private readonly Stopwatch Stopwatch;

	internal readonly IPluginPackage<IModManifest> ModLoaderPackage;
	private ModLoadPhaseState CurrentModLoadPhase = new(ModLoadPhase.BeforeGameAssembly, IsDone: false);
	internal ContentManager? ContentManager { get; private set; }
	private IModManifest? VanillaModManifest;

	internal readonly List<IPluginPackage<IModManifest>> ResolvedMods = [];

	private readonly ConditionalWeakTable<object, Dictionary<string, Dictionary<string, object?>>> ConditionalWeakTableModDataStorage = new();
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
		ExtendableAssemblyDefinitionEditor extendableAssemblyDefinitionEditor,
		Stopwatch stopwatch
	)
	{
		this.InternalModsDirectory = internalModsDirectory;
		this.ModsDirectory = modsDirectory;
		this.ModStorageDirectory = modStorageDirectory;
		this.PrivateModStorageDirectory = privateModStorageDirectory;
		this.LoggerFactory = loggerFactory;
		this.Logger = logger;
		this.Stopwatch = stopwatch;

		this.ModLoaderPackage = new FakePluginPackage(
			manifest: new ModManifest
			{
				UniqueName = NickelConstants.Name,
				Version = NickelConstants.Version,
				DisplayName = NickelConstants.Name,
				Author = NickelConstants.Name,
			},
			packageRoot: new DirectoryInfoImpl(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory))
		);

		this.EventManager = new(
			() => this.CurrentModLoadPhase,
			this.ObtainLogger,
			this.ModLoaderPackage.Manifest
		);
		this.ModDataHandler = new CompoundModDataHandler([
			.. ModDataFieldDefinitionEditor.TypeNamesToAddFieldTo.Select(typeName => new GameTypeDictionaryFieldModDataHandler(typeName, () => this.CurrentModLoadPhase)),
			new ConditionalWeakTableModDataHandler(this.ConditionalWeakTableModDataStorage)
		]);
		this.ModStorageManager = new(this.CreateContractResolver);
		this.EnumCasePool = new();
		this.DelayedHarmonyManager = new();

		var moduleBuilders = new Dictionary<UnorderedPair<string>, ModuleBuilder>();
		this.ProxyManager = new ProxyManager<string>(
			proxyInfo =>
			{
				var key = new UnorderedPair<string>(proxyInfo.Target.Context, proxyInfo.Proxy.Context);
				ref var moduleBuilder = ref CollectionsMarshal.GetValueRefOrAddDefault(moduleBuilders, key, out var exists);
				if (!exists)
				{
					var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"{this.GetType().Namespace}.Proxies{moduleBuilders.Count}, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
					moduleBuilder = assemblyBuilder.DefineDynamicModule($"{this.GetType().Namespace}.Proxies");
				}
				return moduleBuilder!;
			},
			new()
			{
				ProxyPrepareBehavior = ProxyManagerProxyPrepareBehavior.Eager,
				ProxyObjectInterfaceMarking = ProxyObjectInterfaceMarking.IncludeProxyTargetInstance | ProxyObjectInterfaceMarking.IncludeProxyInfo,
				AccessLevelChecking = AccessLevelChecking.DisabledButOnlyAllowPublicMembers,
			}
		);

		var loadContextProvider = new AssemblyModLoadContextProvider(
			AssemblyLoadContext.GetLoadContext(this.GetType().Assembly) ?? AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default,
			extendableAssemblyDefinitionEditor,
			logger
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
						parameterInjector: assemblyPluginLoaderParameterInjector
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
				if (!package.Manifest.AsAssemblyModManifest().TryPickT0(out var assemblyModManifest, out _))
					return null;
				if (assemblyModManifest is null)
					return null;
				
				if (assemblyModManifest.RequiredApiVersion > NickelConstants.Version)
					return new Error<string>(
						$"Mod {package.Manifest.UniqueName} requires API version {assemblyModManifest.RequiredApiVersion}, but {NickelConstants.Name} is currently {NickelConstants.Version}."
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

	private IEnumerable<(IModManifest Manifest, Func<Mod?> Step)> GetLoadModsSteps(ModLoadPhase phase)
	{
		var pluginLoader = new CaseInsensitivePluginLoader<IModManifest, Mod>(
			loader: this.ExtendablePluginLoader
		);
		
		foreach (var package in this.ResolvedMods)
		{
			var manifest = package.Manifest;
			if (this.UniqueNameToInstance.ContainsKey(manifest.UniqueName))
				continue;
			if (this.FailedMods.Contains(manifest))
				continue;
			if (manifest.LoadPhase != phase)
				continue;

			yield return (Manifest: manifest, Step: () =>
			{
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
					return null;
				}

				var canLoadYesNoOrError = pluginLoader.CanLoadPlugin(package);
				if (canLoadYesNoOrError.TryPickT2(out var error, out var canLoadYesOrNo))
				{
					this.FailedMods.Add(manifest);
					this.Logger.LogError("Could not load {DisplayName}: {Error}", displayName, error.Value);
					return null;
				}
				if (canLoadYesOrNo.IsT1)
				{
					this.FailedMods.Add(manifest);
					this.Logger.LogError(
						"Could not load {DisplayName}: no registered loader for this kind of mod.",
						displayName
					);
					return null;
				}

				Mod? result = null;
				try
				{
					pluginLoader.LoadPlugin(package)
						.Switch(
							success =>
							{
								foreach (var warning in success.Warnings)
									this.Logger.LogWarning("{Warning}", warning);

								this.UniqueNameToPackage[manifest.UniqueName] = package;
								this.UniqueNameToInstance[manifest.UniqueName] = success.Plugin;
								this.Logger.LogInformation("Loaded mod {DisplayName}", manifest.GetDisplayName(@long: true));
								result = success.Plugin;
								this.EventManager.OnModLoadedEvent.Raise(null, manifest);
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
				return result;
			});
		}
	}

	public IEnumerable<(string name, Action action)> GetGameLoadQueueStepForModLoadPhase(ModLoadPhase phase)
	{
		List<IModManifest> successfulMods = [];
		
		return [
			(name: $"Nickel::PreModLoadPhase::{phase}", action: () =>
			{
				this.Logger.LogInformation("Loading {Phase} phase mods...", phase);
				this.CurrentModLoadPhase = new(phase, IsDone: false);
				
				// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
				if (DB.currentLocale is null)
					DB.SetLocale(MG.inst.g.settings.locale, MG.inst.g.settings.highResFont);
			}),
			.. this.GetLoadModsSteps(phase).Select<(IModManifest Manifest, Func<Mod?> Step), (string name, Action action)>(step => (name: step.Manifest.GetDisplayName(@long: false), action: () =>
			{
				if (step.Step() is not null)
					successfulMods.Add(step.Manifest);
			})),
			(name: $"Nickel::PostModLoadPhase::{phase}", action: () =>
			{
				this.CurrentModLoadPhase = new(this.CurrentModLoadPhase.Phase, IsDone: true);
				this.Logger.LogInformation("Loaded {Count} mods.", successfulMods.Count);
				this.EventManager.OnModLoadPhaseFinishedEvent.Raise(null, phase);
				this.AfterModLoadPhaseFinished(phase);
			})
		];
	}

	public void LoadMods(ModLoadPhase phase)
	{
		this.Logger.LogInformation("Loading {Phase} phase mods...", phase);
		this.CurrentModLoadPhase = new(phase, IsDone: false);

		var successfulMods = this.GetLoadModsSteps(phase).Select(step => step.Step()).OfType<Mod>().ToList();
		this.CurrentModLoadPhase = new(this.CurrentModLoadPhase.Phase, IsDone: true);
		this.Logger.LogInformation("Loaded {Count} mods.", successfulMods.Count);
		this.EventManager.OnModLoadPhaseFinishedEvent.Raise(null, phase);
		this.AfterModLoadPhaseFinished(phase);
	}

	private void AfterModLoadPhaseFinished(ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterDbInit)
			return;
		
		this.DelayedHarmonyManager.ApplyDelayedPatches();
		this.LogHarmonyPatchesOnce();
		
		this.Logger.LogInformation("Finished loading in {Seconds:#.##}s.", this.Stopwatch.Elapsed.TotalSeconds);
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
		this.VanillaModManifest = new ModManifest
		{
			UniqueName = "CobaltCore",
			Version = gameVersion,
			DisplayName = "Cobalt Core",
			Author = "Rocket Rat Games",
		};

		this.ContentManager = ContentManager.Create(
			() => this.CurrentModLoadPhase,
			this.ObtainLogger,
			this.EventManager,
			this.EnumCasePool,
			this.VanillaModManifest,
			this.ModLoaderPackage.Manifest,
			this.ModDataHandler
		);
		this.PrepareJsonSerialization();
	}

	private void PrepareJsonSerialization()
	{
		var proxyContractResolver = new ProxyContractResolver(this.ProxyManager);

		JSONSettings.indented.Converters.Add(proxyContractResolver);
		JSONSettings.indented.ContractResolver = this.CreateContractResolver(JSONSettings.indented.ContractResolver);

		JSONSettings.serializer.Converters.Add(proxyContractResolver);
		JSONSettings.serializer.ContractResolver = this.CreateContractResolver(JSONSettings.serializer.ContractResolver);
		
		this.ModStorageManager.ClearCache();
	}

	private IContractResolver CreateContractResolver(IContractResolver? wrappedResolver = null)
		=> new ConditionalWeakTableExtensionDataContractResolver(
			new ModificatingContractResolver(
				contractModificator: this.ModifyJsonContract,
				wrapped: wrappedResolver
			),
			this.Logger,
			ModDataFieldDefinitionEditor.JsonPropertyName,
			this.ConditionalWeakTableModDataStorage
		);

	private void ModifyJsonContract(Type type, JsonContract contract)
	{
		if (type.IsAssignableTo(typeof(IProxyObject.IWithProxyTargetInstanceProperty)))
		{
			contract.Converter = new ProxyContractResolver(this.ProxyManager);
			return;
		}
		this.ContentManager?.ModifyJsonContract(type, contract);
	}

	private ILogger ObtainLogger(IModManifest manifest)
	{
		ref var logger = ref CollectionsMarshal.GetValueRefOrAddDefault(this.UniqueNameToLogger, manifest.UniqueName, out var exists);
		if (!exists)
			logger = this.LoggerFactory.CreateLogger(manifest.UniqueName);
		return logger!;
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
		ref var helper = ref CollectionsMarshal.GetValueRefOrAddDefault(this.UniqueNameToHelper, package.Manifest.UniqueName, out var exists);
		if (!exists)
		{
			var modEvents = new ModEvents(package.Manifest, this.EventManager);
			var logger = this.ObtainLogger(package.Manifest);
			
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
					modEvents,
					this.ObtainModHelper,
					this.ObtainLogger,
					mod => this.UniqueNameToPackage[mod.UniqueName]
				),
				modEvents,
				() => new ModContent(
					new ModSprites(package, () => this.ContentManager!.Sprites, logger),
					new ModAudio(package, () => this.ContentManager!.Audio, logger),
					new ModDecks(package.Manifest, () => this.ContentManager!.Decks),
					new ModStatuses(package.Manifest, () => this.ContentManager!.Statuses),
					new ModCards(package.Manifest, () => this.ContentManager!.Cards, () => this.ContentManager!.CardTraits),
					new ModArtifacts(package.Manifest, () => this.ContentManager!.Artifacts),
					new ModCharacters(package.Manifest, () => this.ContentManager!.Characters),
					new ModShips(package.Manifest, () => this.ContentManager!.Ships, () => this.ContentManager!.Parts),
					new ModEnemies(package.Manifest, () => this.ContentManager!.Enemies)
				),
				new ModData(package.Manifest, this.ModDataHandler),
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
		}
		return helper!;
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
