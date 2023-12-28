using Newtonsoft.Json;
using Nickel.Common;

namespace Nickel;

public record struct ModDependency(
    string UniqueName,
    [property: JsonConverter(typeof(SemanticVersionConverter))] SemanticVersion? Version = default,
    bool IsRequired = true
);
