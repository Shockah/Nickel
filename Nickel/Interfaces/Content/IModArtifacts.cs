using System;

namespace Nickel;

public interface IModArtifacts
{
	IArtifactEntry? LookupByArtifactType(Type cardType);
	IArtifactEntry? LookupByUniqueName(string uniqueName);
	IArtifactEntry RegisterArtifact(ArtifactConfiguration configuration);
	IArtifactEntry RegisterArtifact(string name, ArtifactConfiguration configuration);
}
