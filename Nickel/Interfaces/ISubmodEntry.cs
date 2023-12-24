using System.Collections.Generic;

namespace Nickel;

public interface ISubmodEntry
{
    IModManifest Manifest { get; }

    bool IsOptional { get; }

    IReadOnlyDictionary<string, object> ExtensionData { get; }
}
