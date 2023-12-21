using System.Collections.Generic;
using Newtonsoft.Json;
using Shockah.PluginManager;

namespace Nickel;

public sealed class ModManifest : IModManifest
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

    [JsonExtensionData]
    public IReadOnlyDictionary<string, object> ExtensionData { get; private set; } = new Dictionary<string, object>();
}
