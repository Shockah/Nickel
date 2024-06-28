using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager;

/// <summary>
/// The default <see cref="IPluginDependencyResolver{TPluginManifest,TVersion}"/> implementation.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TVersion">The type representing a plugin version.</typeparam>
public sealed class PluginDependencyResolver<TPluginManifest, TVersion> : IPluginDependencyResolver<TPluginManifest, TVersion>
	where TPluginManifest : notnull
	where TVersion : struct, IEquatable<TVersion>, IComparable<TVersion>
{
	private Func<TPluginManifest, RequiredManifestData> RequiredManifestDataProvider { get; }

	/// <summary>
	/// Creates a new <see cref="PluginDependencyResolver{TPluginManifest,TVersion}"/>.
	/// </summary>
	/// <param name="requiredManifestDataProvider">A function which maps plugin manifests to the data required for this resolver.</param>
	public PluginDependencyResolver(Func<TPluginManifest, RequiredManifestData> requiredManifestDataProvider)
	{
		this.RequiredManifestDataProvider = requiredManifestDataProvider;
	}

	/// <inheritdoc/>
	public PluginDependencyResolveResult<TPluginManifest, TVersion> ResolveDependencies(IEnumerable<TPluginManifest> toResolve, IReadOnlySet<TPluginManifest>? resolved = null)
	{
		var toResolveLeft = toResolve.ToList();
		var allResolved = resolved?.ToList() ?? [];

		var manifestEntries = toResolveLeft.Concat(resolved ?? Enumerable.Empty<TPluginManifest>())
			.ToDictionary(m => m, m => new ManifestEntry { Manifest = m, Data = this.RequiredManifestDataProvider(m) });

		#region happy path
		List<IReadOnlySet<TPluginManifest>> loadSteps = [];
		Dictionary<TPluginManifest, PluginDependencyUnresolvableResult<TPluginManifest, TVersion>> unresolvable = [];

		bool MatchesDependency(TPluginManifest manifest, PluginDependency<TVersion> dependency, bool ignoreVersion = false)
		{
			if (!manifestEntries.TryGetValue(manifest, out var entry))
				return false;
			return entry.Data.UniqueName == dependency.UniqueName && (ignoreVersion || dependency.Version is null || dependency.Version.Value.CompareTo(entry.Data.Version) <= 0);
		}

		bool ContainsDependency(IEnumerable<TPluginManifest> enumerable, PluginDependency<TVersion> dependency, bool ignoreVersion = false)
			=> enumerable.Any(m => MatchesDependency(m, dependency, ignoreVersion));

		bool CanDependencyBeResolved(PluginDependency<TVersion> dependency)
			=> ContainsDependency(allResolved, dependency);

		bool CanManifestBeResolved(TPluginManifest manifest, bool onlyRequiredDependencies)
		{
			if (!manifestEntries.TryGetValue(manifest, out var entry))
				return false;
			return entry.Data.Dependencies.Where(d => !onlyRequiredDependencies || d.IsRequired).All(CanDependencyBeResolved);
		}

		while (toResolveLeft.Count > 0)
		{
			var oldCount = toResolveLeft.Count;
			Loop(onlyRequiredDependencies: false);
			Loop(onlyRequiredDependencies: true);
			if (toResolveLeft.Count == oldCount)
				break;

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
		}

		if (toResolveLeft.Count == 0)
			return new PluginDependencyResolveResult<TPluginManifest, TVersion> { LoadSteps = loadSteps, Unresolvable = unresolvable };
		#endregion

		#region manifests unresolvable due to missing dependencies
		HashSet<PluginDependency<TVersion>> GetMissingDependencies(TPluginManifest manifest)
		{
			if (!manifestEntries.TryGetValue(manifest, out var entry))
				return [];
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
				var misverionedDependencies = kvp.Value
					.Where(d => d.Version is not null && (ContainsDependency(allResolved, d, ignoreVersion: true) || ContainsDependency(toResolveLeft, d, ignoreVersion: true)))
					.ToHashSet();
				
				unresolvable[kvp.Key] = new PluginDependencyUnresolvableResult<TPluginManifest, TVersion>.MissingDependencies
				{
					Missing = kvp.Value.Where(d => !misverionedDependencies.Contains(d)).ToHashSet(),
					Misversioned = misverionedDependencies
				};
				toResolveLeft.Remove(kvp.Key);
			}
		}

		if (toResolveLeft.Count == 0)
			return new PluginDependencyResolveResult<TPluginManifest, TVersion> { LoadSteps = loadSteps, Unresolvable = unresolvable };
		#endregion

		#region manifests unresolvable due to dependency cycles
		PluginDependencyChain<TPluginManifest>? FindDependencyCycle(TPluginManifest firstManifest, TPluginManifest? currentManifest = default, List<TPluginManifest>? currentCycle = null)
		{
			currentManifest ??= firstManifest;
			currentCycle ??= [];

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
			List<TPluginManifest> toRemove = [];
			foreach (var manifest in toResolveLeft)
			{
				if (toRemove.Contains(manifest))
					continue;
				if (FindDependencyCycle(manifest) is not { } cycle)
					continue;

				foreach (var cyclePart in cycle.Values)
					unresolvable[cyclePart] = new PluginDependencyUnresolvableResult<TPluginManifest, TVersion>.DependencyCycle { Cycle = cycle };
				toRemove.AddRange(cycle.Values);
			}

			if (toRemove.Count <= 0)
				break;
			_ = toResolveLeft.RemoveAll(toRemove.Contains);
		}
		#endregion

		foreach (var manifest in toResolveLeft)
			unresolvable[manifest] = new PluginDependencyUnresolvableResult<TPluginManifest, TVersion>.UnknownReason();
		return new PluginDependencyResolveResult<TPluginManifest, TVersion> { LoadSteps = loadSteps, Unresolvable = unresolvable };
	}

	/// <summary>
	/// Describes data required for resolving plugin load order via <see cref="PluginDependencyResolver{TPluginManifest,TVersion}"/>.
	/// </summary>
	public readonly struct RequiredManifestData
	{
		/// <summary>The unique name of the plugin.</summary>
		public string UniqueName { get; init; }
		
		/// <summary>The version of the plugin.</summary>
		public TVersion Version { get; init; }
		
		/// <summary>The plugin's dependencies.</summary>
		public IReadOnlySet<PluginDependency<TVersion>> Dependencies { get; init; }
	}

	private readonly struct ManifestEntry
	{
		public TPluginManifest Manifest { get; init; }
		public RequiredManifestData Data { get; init; }
	}
}
