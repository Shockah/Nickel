using System.Collections.Generic;
using Shockah.PluginManager;

namespace Nickel;

internal sealed class AssemblyModManifest : IAssemblyModManifest
{
    public string UniqueName
        => this.ModManifest.UniqueName;

    public SemanticVersion Version
        => this.ModManifest.Version;

    public SemanticVersion RequiredApiVersion
        => this.ModManifest.RequiredApiVersion;

    public IReadOnlySet<PluginDependency> Dependencies
        => this.ModManifest.Dependencies;

    public IReadOnlyDictionary<string, object> ExtensionData
        => this.ModManifest.ExtensionData;

    public string EntryPointAssemblyFileName { get; internal set; } = null!;

    private IModManifest ModManifest { get; init; }

    public AssemblyModManifest(IModManifest modManifest)
    {
        this.ModManifest = modManifest;
    }
}
