using System.Collections.Generic;
using Nanoray.PluginManager;

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

    public string? DisplayName
        => this.ModManifest.DisplayName;

    public string? Author
        => this.ModManifest.Author;

    public string ModType
        => this.ModManifest.ModType;

    public IReadOnlyList<ISubmodEntry> Submods
        => this.ModManifest.Submods;

    public IReadOnlyDictionary<string, object> ExtensionData
        => this.ModManifest.ExtensionData;

    public string EntryPointAssemblyFileName { get; internal set; } = null!;

    public ModLoadPhase LoadPhase { get; internal set; } = ModLoadPhase.AfterGameAssembly;

    private IModManifest ModManifest { get; init; }

    public AssemblyModManifest(IModManifest modManifest)
    {
        this.ModManifest = modManifest;
    }
}
