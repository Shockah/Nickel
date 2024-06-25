using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager;

public sealed class MultiPhasePluginDependencyResolver<TPluginManifest, TVersion, TLoadPhase> : IPluginDependencyResolver<TPluginManifest, TVersion>
	where TPluginManifest : notnull
	where TVersion : struct, IEquatable<TVersion>, IComparable<TVersion>
{
	private IPluginDependencyResolver<TPluginManifest, TVersion> Resolver { get; }
	private Func<TPluginManifest, TLoadPhase> LoadPhaseFunction { get; }
	private IEnumerable<TLoadPhase> LoadPhases { get; }

	public MultiPhasePluginDependencyResolver(
		IPluginDependencyResolver<TPluginManifest, TVersion> resolver,
		Func<TPluginManifest, TLoadPhase> loadPhaseFunction,
		IEnumerable<TLoadPhase> loadPhases
	)
	{
		this.Resolver = resolver;
		this.LoadPhaseFunction = loadPhaseFunction;
		this.LoadPhases = loadPhases;
	}

	public PluginDependencyResolveResult<TPluginManifest, TVersion> ResolveDependencies(IEnumerable<TPluginManifest> toResolve, IReadOnlySet<TPluginManifest>? resolved = null)
	{
		var runtimeResolved = resolved?.ToHashSet() ?? [];
		List<IReadOnlySet<TPluginManifest>> loadSteps = [];
		Dictionary<TPluginManifest, PluginDependencyUnresolvableResult<TPluginManifest, TVersion>> unresolvable = [];

		var toResolveList = toResolve.ToList();
		foreach (var loadPhase in this.LoadPhases)
		{
			var toResolveThisPhase = toResolveList.Where(m => Equals(this.LoadPhaseFunction(m), loadPhase));
			var resolveResultThisPhase = this.Resolver.ResolveDependencies(toResolveThisPhase, runtimeResolved);
			foreach (var (unresolvableManifest, reason) in resolveResultThisPhase.Unresolvable)
				unresolvable[unresolvableManifest] = reason;
			foreach (var resolvedManifest in resolveResultThisPhase.LoadSteps.SelectMany(step => step))
				runtimeResolved.Add(resolvedManifest);
			loadSteps.AddRange(resolveResultThisPhase.LoadSteps);
		}
		return new PluginDependencyResolveResult<TPluginManifest, TVersion> { LoadSteps = loadSteps, Unresolvable = unresolvable };
	}
}
