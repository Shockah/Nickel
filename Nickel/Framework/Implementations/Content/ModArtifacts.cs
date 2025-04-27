using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ModArtifacts(
	IModManifest modManifest,
	Func<ArtifactManager> artifactManagerProvider
) : IModArtifacts
{
	public IReadOnlyDictionary<string, IArtifactEntry> RegisteredArtifacts
		=> this.RegisteredArtifactStorage;
	
	private readonly Dictionary<string, IArtifactEntry> RegisteredArtifactStorage = [];

	public IArtifactEntry? LookupByArtifactType(Type artifactType)
		=> artifactManagerProvider().LookupByArtifactType(artifactType);

	public IArtifactEntry? LookupByUniqueName(string uniqueName)
		=> artifactManagerProvider().LookupByUniqueName(uniqueName);

	public IArtifactEntry RegisterArtifact(ArtifactConfiguration configuration)
	{
		var entry = artifactManagerProvider().RegisterArtifact(modManifest, configuration.ArtifactType.Name, configuration);
		this.RegisteredArtifactStorage[configuration.ArtifactType.Name] = entry;
		return entry;
	}

	public IArtifactEntry RegisterArtifact(string name, ArtifactConfiguration configuration)
	{
		var entry = artifactManagerProvider().RegisterArtifact(modManifest, name, configuration);
		this.RegisteredArtifactStorage[name] = entry;
		return entry;
	}
}
