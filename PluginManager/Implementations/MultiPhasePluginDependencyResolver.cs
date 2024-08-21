using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginDependencyResolver{TPluginManifest,TVersion}"/> which resolves the plugins in multiple phases.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TVersion">The type representing a plugin version.</typeparam>
/// <typeparam name="TLoadPhase">The type representing a load phase.</typeparam>
public sealed class MultiPhasePluginDependencyResolver<TPluginManifest, TVersion, TLoadPhase> : IPluginDependencyResolver<TPluginManifest, TVersion>
	where TPluginManifest : notnull
	where TVersion : struct, IEquatable<TVersion>, IComparable<TVersion>
{
	private readonly IPluginDependencyResolver<TPluginManifest, TVersion> Resolver;
	private readonly Func<TPluginManifest, TLoadPhase> LoadPhaseFunction;
	private readonly IReadOnlyList<TLoadPhase> LoadPhases;

	/// <summary>
	/// Creates a new <see cref="MultiPhasePluginDependencyResolver{TPluginManifest,TVersion,TLoadPhase}"/>.
	/// </summary>
	/// <param name="resolver">The underlying resolver.</param>
	/// <param name="loadPhaseFunction">A function controlling which phase a plugin is to be loaded in.</param>
	/// <param name="loadPhases">A list of all load phases.</param>
	public MultiPhasePluginDependencyResolver(
		IPluginDependencyResolver<TPluginManifest, TVersion> resolver,
		Func<TPluginManifest, TLoadPhase> loadPhaseFunction,
		IReadOnlyList<TLoadPhase> loadPhases
	)
	{
		this.Resolver = resolver;
		this.LoadPhaseFunction = loadPhaseFunction;
		this.LoadPhases = loadPhases;
	}

	/// <inheritdoc/>
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
