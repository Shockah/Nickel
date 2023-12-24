using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nickel;

internal sealed class SubmodEntry : ISubmodEntry
{
    [JsonProperty]
    [JsonRequired]
    [JsonConverter(typeof(ConcreteTypeConverter<ModManifest>))]
    public IModManifest Manifest { get; private set; } = null!;

    [JsonProperty]
    public bool IsOptional { get; private set; } = true;

    [JsonExtensionData]
    public IReadOnlyDictionary<string, object> ExtensionData { get; private set; } = new Dictionary<string, object>();
}
