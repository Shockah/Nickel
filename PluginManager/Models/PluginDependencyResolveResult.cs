using System.Collections.Generic;
using OneOf;

namespace Shockah.PluginManager;

public readonly struct PluginDependencyResolveResult<TPluginManifest>
    where TPluginManifest : notnull
{
    public IReadOnlyList<IReadOnlySet<TPluginManifest>> LoadSteps { get; init; }
    public IReadOnlyDictionary<TPluginManifest, PluginDependencyUnresolvableResult<TPluginManifest>> Unresolvable { get; init; }
}

[GenerateOneOf]
public partial class PluginDependencyUnresolvableResult<TPluginManifest> : OneOfBase<
    PluginDependencyUnresolvableResult<TPluginManifest>.MissingDependencies,
    PluginDependencyUnresolvableResult<TPluginManifest>.DependencyCycle,
    PluginDependencyUnresolvableResult<TPluginManifest>.UnknownReason
>
{
    public record struct MissingDependencies(
        IReadOnlySet<PluginDependency> Dependencies
    );

    public record struct DependencyCycle(
        PluginDependencyChain<TPluginManifest> Cycle
    );

    public struct UnknownReason { }
}
