using System.Collections.Generic;
using Newtonsoft.Json;
using Shockah.PluginManager;

namespace Nickel;

internal sealed class ModManifest : IModManifest
{
    [JsonProperty]
    [JsonRequired]
    public string UniqueName { get; private set; } = null!;

    [JsonProperty]
    [JsonRequired]
    public SemanticVersion Version { get; private set; } = default;

    [JsonProperty]
    [JsonRequired]
    public SemanticVersion RequiredApiVersion { get; private set; } = default;

    [JsonProperty]
    public IReadOnlySet<PluginDependency> Dependencies { get; private set; } = new HashSet<PluginDependency>();

    [JsonProperty]
    public string? DisplayName { get; private set; } = null;

    [JsonProperty]
    public string? Author { get; private set; } = null;

    [JsonExtensionData]
    public IReadOnlyDictionary<string, object> ExtensionData { get; private set; } = new Dictionary<string, object>();
}
