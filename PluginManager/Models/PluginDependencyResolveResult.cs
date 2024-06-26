using OneOf;
using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// The result of <see cref="IPluginDependencyResolver{TPluginManifest,TVersion}"/>.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TVersion">The type representing a plugin version.</typeparam>
public readonly struct PluginDependencyResolveResult<TPluginManifest, TVersion>
	where TPluginManifest : notnull
	where TVersion : struct, IEquatable<TVersion>, IComparable<TVersion>
{
	/// <summary>
	/// The steps in which plugins should be loaded. All plugins of one step have to be loaded, before another step can be started.
	/// </summary>
	public IReadOnlyList<IReadOnlySet<TPluginManifest>> LoadSteps { get; init; }
	
	/// <summary>
	/// A collection of plugins that could not have their load order be resolved, along with the reasons.
	/// </summary>
	public IReadOnlyDictionary<TPluginManifest, PluginDependencyUnresolvableResult<TPluginManifest, TVersion>> Unresolvable { get; init; }
}

/// <summary>
/// Describes a set of reasons why a plugin could not have their load order be resolved.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TVersion">The type representing a plugin version.</typeparam>
[GenerateOneOf]
public partial class PluginDependencyUnresolvableResult<TPluginManifest, TVersion> : OneOfBase<
	PluginDependencyUnresolvableResult<TPluginManifest, TVersion>.MissingDependencies,
	PluginDependencyUnresolvableResult<TPluginManifest, TVersion>.DependencyCycle,
	PluginDependencyUnresolvableResult<TPluginManifest, TVersion>.UnknownReason
>
	where TVersion : struct, IEquatable<TVersion>, IComparable<TVersion>
{
	/// <summary>
	/// The plugin requires dependencies which are not available.
	/// </summary>
	/// <param name="Dependencies">The dependencies that were not available.</param>
	public record struct MissingDependencies(
		IReadOnlySet<PluginDependency<TVersion>> Dependencies
	);

	/// <summary>
	/// The plugin requires itself to be loaded, directly or indirectly.
	/// </summary>
	/// <param name="Cycle">The actual cycle of mods depending on each other.</param>
	public record struct DependencyCycle(
		PluginDependencyChain<TPluginManifest> Cycle
	);

	/// <summary>
	/// The plugin's load order could not be resolved for some other reason.
	/// </summary>
	public struct UnknownReason;
}
