using System;
using System.Collections.Generic;
using OneOf;

namespace Nanoray.PluginManager;

public readonly struct PluginDependencyResolveResult<TPluginManifest, TVersion>
    where TPluginManifest : notnull
    where TVersion : struct, IEquatable<TVersion>, IComparable<TVersion>
{
    public IReadOnlyList<IReadOnlySet<TPluginManifest>> LoadSteps { get; init; }
    public IReadOnlyDictionary<TPluginManifest, PluginDependencyUnresolvableResult<TPluginManifest, TVersion>> Unresolvable { get; init; }
}

[GenerateOneOf]
public partial class PluginDependencyUnresolvableResult<TPluginManifest, TVersion> : OneOfBase<
    PluginDependencyUnresolvableResult<TPluginManifest, TVersion>.MissingDependencies,
    PluginDependencyUnresolvableResult<TPluginManifest, TVersion>.DependencyCycle,
    PluginDependencyUnresolvableResult<TPluginManifest, TVersion>.UnknownReason
>
    where TVersion : struct, IEquatable<TVersion>, IComparable<TVersion>
{
    public record struct MissingDependencies(
        IReadOnlySet<PluginDependency<TVersion>> Dependencies
    );

    public record struct DependencyCycle(
        PluginDependencyChain<TPluginManifest> Cycle
    );

    public struct UnknownReason { }
}
