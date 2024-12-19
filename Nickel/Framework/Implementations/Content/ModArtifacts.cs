using System;

namespace Nickel;

internal sealed class ModArtifacts(
	IModManifest modManifest,
	Func<ArtifactManager> artifactManagerProvider
) : IModArtifacts
{
	public IArtifactEntry? LookupByArtifactType(Type artifactType)
		=> artifactManagerProvider().LookupByArtifactType(artifactType);

	public IArtifactEntry? LookupByUniqueName(string uniqueName)
		=> artifactManagerProvider().LookupByUniqueName(uniqueName);

	public IArtifactEntry RegisterArtifact(ArtifactConfiguration configuration)
		=> artifactManagerProvider().RegisterArtifact(modManifest, configuration.ArtifactType.Name, configuration);

	public IArtifactEntry RegisterArtifact(string name, ArtifactConfiguration configuration)
		=> artifactManagerProvider().RegisterArtifact(modManifest, name, configuration);
}
