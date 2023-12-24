using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager;

public sealed class PluginDependencyResolver<TPluginManifest> : IPluginDependencyResolver<TPluginManifest>
    where TPluginManifest : notnull
{
    private Func<TPluginManifest, RequiredManifestData> RequiredManifestDataProvider { get; init; }

    public PluginDependencyResolver(Func<TPluginManifest, RequiredManifestData> requiredManifestDataProvider)
    {
        this.RequiredManifestDataProvider = requiredManifestDataProvider;
    }

    public PluginDependencyResolveResult<TPluginManifest> ResolveDependencies(IEnumerable<TPluginManifest> toResolve, IReadOnlySet<TPluginManifest>? resolved = null)
    {
        Dictionary<TPluginManifest, ManifestEntry> manifestEntries = toResolve.Concat(resolved ?? Enumerable.Empty<TPluginManifest>())
            .ToDictionary(m => m, m => new ManifestEntry { Manifest = m, Data = this.RequiredManifestDataProvider(m) });
        Dictionary<string, ManifestEntry> manifestEntriesByName = manifestEntries.Values
            .ToDictionary(m => m.Data.UniqueName, m => m);

        #region happy path
        List<IReadOnlySet<TPluginManifest>> loadSteps = new();
        Dictionary<TPluginManifest, PluginDependencyUnresolvableResult<TPluginManifest>> unresolvable = new();
        List<TPluginManifest> allResolved = resolved?.ToList() ?? new();
        List<TPluginManifest> toResolveLeft = toResolve.ToList();

        bool MatchesDependency(TPluginManifest manifest, PluginDependency dependency)
        {
            if (!manifestEntries.TryGetValue(manifest, out var entry))
                return false;
            return entry.Data.UniqueName == dependency.UniqueName && (dependency.Version is null || dependency.Version <= entry.Data.Version);
        }

        bool ContainsDependency(IEnumerable<TPluginManifest> enumerable, PluginDependency dependency)
            => enumerable.Any(m => MatchesDependency(m, dependency));

        bool CanDependencyBeResolved(PluginDependency dependency)
            => ContainsDependency(allResolved, dependency);

        bool CanManifestBeResolved(TPluginManifest manifest, bool onlyRequiredDependencies)
        {
            if (!manifestEntries.TryGetValue(manifest, out var entry))
                return false;
            return entry.Data.Dependencies.Where(d => !onlyRequiredDependencies || d.IsRequired).All(CanDependencyBeResolved);
        }

        while (toResolveLeft.Count > 0)
        {
            void Loop(bool onlyRequiredDependencies)
            {
                while (toResolveLeft.Count > 0)
                {
                    var loadStep = toResolveLeft.Where(m => CanManifestBeResolved(m, onlyRequiredDependencies: onlyRequiredDependencies)).ToHashSet();
                    if (loadStep.Count == 0)
                        break;

                    loadSteps.Add(loadStep);
                    allResolved.AddRange(loadStep);
                    toResolveLeft.RemoveAll(loadStep.Contains);
                }
            }

            Loop(onlyRequiredDependencies: false);
            Loop(onlyRequiredDependencies: true);
        }

        if (toResolveLeft.Count == 0)
            return new PluginDependencyResolveResult<TPluginManifest> { LoadSteps = loadSteps, Unresolvable = unresolvable };
        #endregion

        #region manifests unresolvable due to missing dependencies
        HashSet<PluginDependency> GetMissingDependencies(TPluginManifest manifest)
        {
            if (!manifestEntries.TryGetValue(manifest, out var entry))
                return new();
            return entry.Data.Dependencies
                .Where(d => !ContainsDependency(allResolved, d) && !ContainsDependency(toResolveLeft, d))
                .ToHashSet();
        }

        while (toResolveLeft.Count > 0)
        {
            var missingDependencyManifests = toResolveLeft
                .Select(m => (Manifest: m, Dependencies: GetMissingDependencies(m)))
                .Where(e => e.Dependencies.Count > 0)
                .ToDictionary(e => e.Manifest, e => e.Dependencies);
            if (missingDependencyManifests.Count <= 0)
                break;

            foreach (var kvp in missingDependencyManifests)
            {
                unresolvable[kvp.Key] = new PluginDependencyUnresolvableResult<TPluginManifest>.MissingDependencies { Dependencies = kvp.Value };
                toResolveLeft.Remove(kvp.Key);
            }
        }

        if (toResolveLeft.Count == 0)
            return new PluginDependencyResolveResult<TPluginManifest> { LoadSteps = loadSteps, Unresolvable = unresolvable };
        #endregion

        #region manifests unresolvable due to dependency cycles
        PluginDependencyChain<TPluginManifest>? FindDependencyCycle(TPluginManifest firstManifest, TPluginManifest? currentManifest = default, List<TPluginManifest>? currentCycle = null)
        {
            currentManifest ??= firstManifest;
            currentCycle ??= new();

            if (Equals(currentManifest, firstManifest) && currentCycle.Count > 0)
                return new() { Values = currentCycle };
            if (!manifestEntries.TryGetValue(currentManifest, out var entry))
                return null;
            foreach (var dependency in entry.Data.Dependencies)
            {
                var matchingManifest = toResolveLeft.FirstOrDefault(m => MatchesDependency(m, dependency));
                if (matchingManifest is null)
                    continue;
                var newCycle = currentCycle.Append(matchingManifest).ToList();
                var fullCycle = FindDependencyCycle(firstManifest, matchingManifest, newCycle);
                if (fullCycle is not null)
                    return fullCycle;
            }
            return null;
        }

        while (toResolveLeft.Count > 0)
        {
            List<TPluginManifest> toRemove = new();
            foreach (var manifest in toResolveLeft)
            {
                if (toRemove.Contains(manifest))
                    continue;
                if (FindDependencyCycle(manifest) is not { } cycle)
                    continue;

                foreach (var cyclePart in cycle.Values)
                    unresolvable[cyclePart] = new PluginDependencyUnresolvableResult<TPluginManifest>.DependencyCycle { Cycle = cycle };
                toRemove.AddRange(cycle.Values);
            }

            if (toRemove.Count <= 0)
                break;
            _ = toResolveLeft.RemoveAll(toRemove.Contains);
        }
        #endregion

        foreach (var manifest in toResolveLeft)
            unresolvable[manifest] = new PluginDependencyUnresolvableResult<TPluginManifest>.UnknownReason();
        return new PluginDependencyResolveResult<TPluginManifest> { LoadSteps = loadSteps, Unresolvable = unresolvable };
    }

    public readonly struct RequiredManifestData
    {
        public string UniqueName { get; init; }
        public SemanticVersion Version { get; init; }
        public IReadOnlySet<PluginDependency> Dependencies { get; init; }
    }

    private readonly struct ManifestEntry
    {
        public TPluginManifest Manifest { get; init; }
        public RequiredManifestData Data { get; init; }
    }
}
