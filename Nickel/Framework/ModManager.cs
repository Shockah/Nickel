using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Pintail;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using Nanoray.PluginManager.Implementations;
using Newtonsoft.Json.Serialization;
using Nickel.Common;
using Nickel.Framework;
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
	private DirectoryInfo ModsDirectory { get; }
	private ILoggerFactory LoggerFactory { get; }
	internal ILogger Logger { get; }
	internal ModEventManager EventManager { get; }
	internal ModDataManager ModDataManager { get; }

	internal IModManifest ModLoaderModManifest { get; private init; }
	internal ModLoadPhase CurrentModLoadPhase { get; private set; } = ModLoadPhase.BeforeGameAssembly;
	internal ContentManager? ContentManager { get; private set; }
	internal IModManifest? VanillaModManifest { get; private set; }

	internal List<IPluginPackage<IModManifest>> ResolvedMods { get; } = [];

	private ExtendablePluginLoader<IModManifest, Mod> ExtendablePluginLoader { get; } = new();
	private IProxyManager<string> ProxyManager { get; }
	private List<IModManifest> FailedMods { get; } = [];
	private HashSet<IModManifest> OptionalSubmods { get; } = [];
	private bool DidLogHarmonyPatchesOnce = false;

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

		this.ModLoaderModManifest = new ModManifest()
		{
			UniqueName = NickelConstants.Name,
			Version = NickelConstants.Version,
			DisplayName = NickelConstants.Name,
			Author = NickelConstants.Name,
			RequiredApiVersion = NickelConstants.Version
		};

		this.EventManager = new(
			() => this.CurrentModLoadPhase,
			this.ObtainLogger,
			this.ModLoaderModManifest
		);
		this.ModDataManager = new();

		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"{this.GetType().Namespace}.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
		var moduleBuilder = assemblyBuilder.DefineDynamicModule($"{this.GetType().Namespace}.Proxies");
		this.ProxyManager = new ProxyManager<string>(moduleBuilder, new ProxyManagerConfiguration<string>(
			proxyPrepareBehavior: ProxyManagerProxyPrepareBehavior.Eager,
			proxyObjectInterfaceMarking: ProxyObjectInterfaceMarking.MarkerWithProperty,
			accessLevelChecking: AccessLevelChecking.DisabledButOnlyAllowPublicMembers
		));

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
					manifestFileName: NickelConstants.ManifestFileName,
					ignoreDotNames: true,
					allowPluginsInRoot: allowModsInRoot,
					directoryResolverFactory: d => new DirectoryPluginPackageResolver<IModManifest>(d, NickelConstants.ManifestFileName, pluginManifestLoader, SingleFilePluginPackageResolverNoManifestResult.Empty),
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

		var dependencyResolverResult = pluginDependencyResolver.ResolveDependencies(
			toLoad
				.Select(p => p.Manifest)
				.OrderBy(m => m.UniqueName)
		);
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

			var displayName = manifest.GetDisplayName(@long: false);
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

		this.Logger.LogInformation("Loaded {Count} mods.", successfulMods.Count);
		this.EventManager.OnModLoadPhaseFinishedEvent.Raise(null, phase);
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
						if (patchInfo.Prefixes.Any(p => p.owner == owner))
							patchTypeStrings.Add("prefix");
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

		this.ContentManager = ContentManager.Create(() => this.CurrentModLoadPhase, this.ObtainLogger, this.VanillaModManifest, this.ModLoaderModManifest, this.ModDataManager);
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

	private IModHelper ObtainModHelper(IPluginPackage<IModManifest> package)
	{
		if (!this.UniqueNameToHelper.TryGetValue(package.Manifest.UniqueName, out var helper))
		{
			helper = new ModHelper(
				new ModRegistry(
					package.Manifest,
					() => this.VanillaModManifest,
					this.ModsDirectory,
					this.UniqueNameToInstance,
					this.UniqueNameToPackage,
					this.ProxyManager
				),
				new ModEvents(package.Manifest, this.EventManager),
				() => new ModContent(
					new ModSprites(package, () => this.ContentManager!.Sprites),
					new ModDecks(package.Manifest, () => this.ContentManager!.Decks),
					new ModStatuses(package.Manifest, () => this.ContentManager!.Statuses),
					new ModCards(package.Manifest, () => this.ContentManager!.Cards, () => this.ContentManager!.CardTraits),
					new ModArtifacts(package.Manifest, () => this.ContentManager!.Artifacts),
					new ModCharacters(package.Manifest, () => this.ContentManager!.Characters),
					new ModShips(
						package.Manifest,
						() => this.ContentManager!.Ships,
						() => this.ContentManager!.Parts
					)
				),
				new ModData(package.Manifest, this.ModDataManager),
				() => this.CurrentModLoadPhase
			);
			this.UniqueNameToHelper[package.Manifest.UniqueName] = helper;
		}

		return helper;
	}
}
