using System.Collections.Generic;
using Newtonsoft.Json;
using Nanoray.PluginManager;

namespace Nickel;

internal sealed class ModManifest : IModManifest
{
    [JsonProperty]
    [JsonRequired]
    public string UniqueName { get; internal set; } = null!;

    [JsonProperty]
    [JsonRequired]
    public SemanticVersion Version { get; internal set; } = default;

    [JsonProperty]
    [JsonRequired]
    public SemanticVersion RequiredApiVersion { get; internal set; } = default;

    [JsonProperty]
    public IReadOnlySet<PluginDependency> Dependencies { get; internal set; } = new HashSet<PluginDependency>();

    [JsonProperty]
    public string? DisplayName { get; internal set; } = null;

    [JsonProperty]
    public string? Author { get; internal set; } = null;

    [JsonProperty]
    public string ModType { get; internal set; } = $"{typeof(ModManifest).Namespace!}.Assembly";

    [JsonProperty]
    [JsonConverter(typeof(ConcreteTypeConverter<IReadOnlyList<SubmodEntry>>))]
    public IReadOnlyList<ISubmodEntry> Submods { get; internal set; } = new List<ISubmodEntry>();

    [JsonExtensionData]
    public IDictionary<string, object> ExtensionData { get; internal set; } = new Dictionary<string, object>();

    IReadOnlyDictionary<string, object> IModManifest.ExtensionData
        => (IReadOnlyDictionary<string, object>)ExtensionData;
}
