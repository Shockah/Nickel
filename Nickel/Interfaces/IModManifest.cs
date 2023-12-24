using System.Collections.Generic;
using Nanoray.PluginManager;

namespace Nickel;

public interface IModManifest
{
    string UniqueName { get; }

    SemanticVersion Version { get; }

    SemanticVersion RequiredApiVersion { get; }

    IReadOnlySet<PluginDependency> Dependencies { get; }

    string? DisplayName { get; }

    string? Author { get; }

    string ModType { get; }

    IReadOnlyList<ISubmodEntry> Submods { get; }

    IReadOnlyDictionary<string, object> ExtensionData { get; }
}
