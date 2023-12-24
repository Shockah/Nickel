using System.Collections.Generic;
using Newtonsoft.Json;
using Nanoray.PluginManager;

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

    [JsonProperty]
    public string ModType { get; private set; } = $"{typeof(ModManifest).Namespace!}.Assembly";

    [JsonProperty]
    [JsonConverter(typeof(ConcreteTypeConverter<IReadOnlyList<SubmodEntry>>))]
    public IReadOnlyList<ISubmodEntry> Submods { get; private set; } = new List<ISubmodEntry>();

    [JsonExtensionData]
    public IReadOnlyDictionary<string, object> ExtensionData { get; private set; } = new Dictionary<string, object>();
}
