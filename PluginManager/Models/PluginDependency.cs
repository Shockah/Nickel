namespace Nanoray.PluginManager;

public record struct PluginDependency(
    string UniqueName,
    SemanticVersion? Version = null,
    bool IsRequired = true
);
