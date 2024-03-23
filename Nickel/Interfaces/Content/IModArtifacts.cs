using System;

namespace Nickel;

/// <summary>
/// A mod-specific artifact registry.
/// Allows looking up and registering artifacts.
/// </summary>
public interface IModArtifacts
{
	IArtifactEntry? LookupByArtifactType(Type cardType);
	IArtifactEntry? LookupByUniqueName(string uniqueName);
	IArtifactEntry RegisterArtifact(ArtifactConfiguration configuration);
	IArtifactEntry RegisterArtifact(string name, ArtifactConfiguration configuration);
}
