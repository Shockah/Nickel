using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Shockah.PluginManager;

namespace Nickel;

internal sealed class ModManager
{
    private DirectoryInfo ModsDirectory { get; }
    private ILogger Logger { get; init; }

    private Dictionary<string, ILogger> UniqueNameToLogger { get; init; } = new();
    private Dictionary<string, IModHelper> UniqueNameToHelper { get; init; } = new();
    private Dictionary<string, Mod> UniqueNameToInstance { get; init; } = new();

    public ModManager(DirectoryInfo modsDirectory, ILogger logger)
    {
        this.ModsDirectory = modsDirectory;
        this.Logger = logger;
    }

    public void LoadMods()
    {
        this.Logger.LogInformation("Resolving mods...");

        var extendablePluginLoader = new ExtendablePluginLoader<IModManifest, Mod>();

        var pluginDependencyResolver = new PluginDependencyResolver<IModManifest>(
            requiredManifestDataProvider: p => new PluginDependencyResolver<IModManifest>.RequiredManifestData { UniqueName = p.UniqueName, Version = p.Version, Dependencies = p.Dependencies }
        );
        var pluginManifestLoader = new JsonPluginManifestLoader<ModManifest>();
        var pluginPackageResolver = new RecursiveDirectoryPluginPackageResolver<IModManifest>(
            directory: this.ModsDirectory,
            manifestFileName: "nickel.json",
            ignoreDotDirectories: true,
            pluginManifestLoader: new SpecializedPluginManifestLoader<ModManifest, IModManifest>(pluginManifestLoader)
        );
        var assemblyPluginLoaderParameterInjector = new CompoundAssemblyPluginLoaderParameterInjector<IModManifest>(
            new DelegateAssemblyPluginLoaderParameterInjector<IModManifest, ILogger>(package => ObtainLogger(package.Manifest)),
            new DelegateAssemblyPluginLoaderParameterInjector<IModManifest, IModHelper>(package => ObtainModHelper(package.Manifest)),
            new ValueAssemblyPluginLoaderParameterInjector<IModManifest, ExtendablePluginLoader<IModManifest, Mod>>(extendablePluginLoader)
        );
        var assemblyPluginLoader = new AssemblyPluginLoader<IAssemblyModManifest, Mod>(
            requiredPluginDataProvider: p => new AssemblyPluginLoader<IAssemblyModManifest, Mod>.RequiredPluginData { UniqueName = p.Manifest.UniqueName, EntryPointAssemblyFileName = p.Manifest.EntryPointAssemblyFileName },
            parameterInjector: assemblyPluginLoaderParameterInjector
        );

        extendablePluginLoader.RegisterPluginLoader(new SpecializedManifestPluginLoader<IAssemblyModManifest, IModManifest, Mod>(assemblyPluginLoader));

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

        this.Logger.LogInformation("Loading mods...");

        List<IModManifest> failedMods = new();
        foreach (var step in dependencyResolverResult.LoadSteps)
        {
            foreach (var manifest in step)
            {
                var failedRequiredDependencies = manifest.Dependencies.Where(d => d.IsRequired && failedMods.Any(m => m.UniqueName == d.UniqueName)).ToList();
                if (failedRequiredDependencies.Count > 0)
                {
                    failedMods.Add(manifest);
                    this.Logger.LogError("Could not load {UniqueName}: Required dependencies failed to load: {Dependencies}", manifest.UniqueName, string.Join(", ", failedRequiredDependencies.Select(d => d.UniqueName)));
                    continue;
                }

                var package = toLoad.FirstOrDefault(p => p.Manifest.UniqueName == manifest.UniqueName) ?? throw new InvalidOperationException();
                if (!extendablePluginLoader.CanLoadPlugin(package))
                {
                    failedMods.Add(manifest);
                    this.Logger.LogError("Could not load {UniqueName}: no registered loader for this kind of mod.", manifest.UniqueName);
                    continue;
                }

                extendablePluginLoader.LoadPlugin(package).Switch(
                    mod =>
                    {
                        this.UniqueNameToInstance[manifest.UniqueName] = mod;
                        mod.Package = package;
                        mod.Logger = this.ObtainLogger(manifest);
                        mod.Helper = this.ObtainModHelper(manifest);
                        this.Logger.LogInformation("Loaded mod {UniqueName}.", manifest.UniqueName);
                    },
                    error =>
                    {
                        failedMods.Add(manifest);
                        this.Logger.LogError("Could not load {UniqueName}: {Error}", manifest.UniqueName, error.Value);
                    }
                );
            }
        }
    }

    private ILogger ObtainLogger(IModManifest manifest)
    {
        if (!this.UniqueNameToLogger.TryGetValue(manifest.UniqueName, out var logger))
        {
            // TODO: cache LoggerFactory
            logger = LoggerFactory.Create(b => { }).CreateLogger(manifest.UniqueName);
            this.UniqueNameToLogger[manifest.UniqueName] = logger;
        }
        return logger;
    }

    private IModHelper ObtainModHelper(IModManifest manifest)
    {
        if (!this.UniqueNameToHelper.TryGetValue(manifest.UniqueName, out var helper))
        {
            ModRegistry modRegistry = new(manifest, this.UniqueNameToInstance);
            helper = new ModHelper(modRegistry);
            this.UniqueNameToHelper[manifest.UniqueName] = helper;
        }
        return helper;
    }
}
