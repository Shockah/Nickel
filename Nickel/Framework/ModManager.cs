using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;

namespace Nickel;

internal sealed class ModManager
{
    private DirectoryInfo ModsDirectory { get; init; }
    private ILoggerFactory LoggerFactory { get; init; }
    private ILogger Logger { get; init; }
    internal ModEventManager EventManager { get; private init; }

    private ExtendablePluginLoader<IModManifest, Mod> ExtendablePluginLoader { get; init; } = new();
    private List<IPluginPackage<IModManifest>> ResolvedMods { get; init; } = new();
    private List<IModManifest> FailedMods { get; init; } = new();

    private Dictionary<string, ILogger> UniqueNameToLogger { get; init; } = new();
    private Dictionary<string, IModHelper> UniqueNameToHelper { get; init; } = new();
    private Dictionary<string, Mod> UniqueNameToInstance { get; init; } = new();

    public ModManager(DirectoryInfo modsDirectory, ILoggerFactory loggerFactory, ILogger logger)
    {
        this.ModsDirectory = modsDirectory;
        this.LoggerFactory = loggerFactory;
        this.Logger = logger;
        this.EventManager = new(ObtainLogger);

        var assemblyPluginLoaderParameterInjector = new CompoundAssemblyPluginLoaderParameterInjector<IModManifest>(
            new DelegateAssemblyPluginLoaderParameterInjector<IModManifest, ILogger>(package => ObtainLogger(package.Manifest)),
            new DelegateAssemblyPluginLoaderParameterInjector<IModManifest, IModHelper>(package => ObtainModHelper(package.Manifest)),
            new ValueAssemblyPluginLoaderParameterInjector<IModManifest, ExtendablePluginLoader<IModManifest, Mod>>(this.ExtendablePluginLoader)
        );

        var assemblyPluginLoader = new AssemblyPluginLoader<IAssemblyModManifest, Mod>(
            requiredPluginDataProvider: p => new AssemblyPluginLoader<IAssemblyModManifest, Mod>.RequiredPluginData
            {
                UniqueName = p.Manifest.UniqueName,
                EntryPointAssemblyFileName = p.Manifest.EntryPointAssemblyFileName
            },
            parameterInjector: assemblyPluginLoaderParameterInjector
        );

        this.ExtendablePluginLoader.RegisterPluginLoader(
            new SpecializedConvertingManifestPluginLoader<IAssemblyModManifest, IModManifest, Mod>(
                assemblyPluginLoader,
                m => m.AsAssemblyModManifest()
            )
        );
    }

    private static ModLoadPhase GetModLoadPhaseForManifest(IModManifest manifest)
        => (manifest as IAssemblyModManifest)?.LoadPhase ?? ModLoadPhase.AfterGameAssembly;

    public void ResolveMods()
    {
        this.Logger.LogInformation("Resolving mods...");

        var pluginDependencyResolver = new MultiPhasePluginDependencyResolver<IModManifest, ModLoadPhase>(
            new PluginDependencyResolver<IModManifest>(
                requiredManifestDataProvider: p => new PluginDependencyResolver<IModManifest>.RequiredManifestData { UniqueName = p.UniqueName, Version = p.Version, Dependencies = p.Dependencies }
            ),
            GetModLoadPhaseForManifest,
            Enum.GetValues<ModLoadPhase>()
        );
        var pluginManifestLoader = new JsonPluginManifestLoader<ModManifest>();
        var pluginPackageResolver = new RecursiveDirectoryPluginPackageResolver<IModManifest>(
            directory: this.ModsDirectory,
            manifestFileName: "nickel.json",
            ignoreDotDirectories: true,
            pluginManifestLoader: new SpecializedPluginManifestLoader<ModManifest, IModManifest>(pluginManifestLoader)
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
            reason.Switch(
                missingDependencies => this.Logger.LogError("Could not load {UniqueName}: missing dependencies: {Dependencies}", unresolvableManifest.UniqueName, string.Join(", ", missingDependencies.Dependencies.Select(d => d.UniqueName))),
                dependencyCycle => this.Logger.LogError("Could not load {UniqueName}: dependency cycle: {Cycle}", unresolvableManifest.UniqueName, string.Join(" -> ", dependencyCycle.Cycle.Values.Append(dependencyCycle.Cycle.Values[0]).Select(m => m.UniqueName))),
                unknown => this.Logger.LogError("Could not load {UniqueName}: unknown reason.", unresolvableManifest.UniqueName)
            );

        this.ResolvedMods.AddRange(
            dependencyResolverResult.LoadSteps
                .SelectMany(step => step)
                .Select(m => toLoad.FirstOrDefault(p => p.Manifest.UniqueName == m.UniqueName) ?? throw new InvalidOperationException())
        );
    }

    public void LoadMods(ModLoadPhase phase)
    {
        this.Logger.LogInformation("Loading {Phase} phase mods...", phase);

        List<IModManifest> successfulMods = new();
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

            var failedRequiredDependencies = manifest.Dependencies.Where(d => d.IsRequired && this.FailedMods.Any(m => m.UniqueName == d.UniqueName)).ToList();
            if (failedRequiredDependencies.Count > 0)
            {
                this.FailedMods.Add(manifest);
                this.Logger.LogError("Could not load {UniqueName}: Required dependencies failed to load: {Dependencies}", manifest.UniqueName, string.Join(", ", failedRequiredDependencies.Select(d => d.UniqueName)));
                continue;
            }

            if (!this.ExtendablePluginLoader.CanLoadPlugin(package))
            {
                this.FailedMods.Add(manifest);
                this.Logger.LogError("Could not load {UniqueName}: no registered loader for this kind of mod.", manifest.UniqueName);
                continue;
            }

            this.ExtendablePluginLoader.LoadPlugin(package).Switch(
                mod =>
                {
                    this.UniqueNameToInstance[manifest.UniqueName] = mod;
                    mod.Package = package;
                    mod.Logger = this.ObtainLogger(manifest);
                    mod.Helper = this.ObtainModHelper(manifest);
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
            ModRegistry modRegistry = new(manifest, this.UniqueNameToInstance);
            ModEvents modEvents = new(manifest, this.EventManager);
            helper = new ModHelper(modRegistry, modEvents);
            this.UniqueNameToHelper[manifest.UniqueName] = helper;
        }
        return helper;
    }
}
