using System;

namespace Nickel;

internal sealed class ModArtifacts : IModArtifacts
{
	private readonly IModManifest ModManifest;
	private readonly Func<ArtifactManager> ArtifactManagerProvider;

	public ModArtifacts(IModManifest modManifest, Func<ArtifactManager> artifactManagerProvider)
	{
		this.ModManifest = modManifest;
		this.ArtifactManagerProvider = artifactManagerProvider;
	}

	public IArtifactEntry? LookupByArtifactType(Type artifactType)
		=> this.ArtifactManagerProvider().LookupByArtifactType(artifactType);

	public IArtifactEntry? LookupByUniqueName(string uniqueName)
		=> this.ArtifactManagerProvider().LookupByUniqueName(uniqueName);

	public IArtifactEntry RegisterArtifact(ArtifactConfiguration configuration)
		=> this.ArtifactManagerProvider().RegisterArtifact(this.ModManifest, configuration.ArtifactType.Name, configuration);

	public IArtifactEntry RegisterArtifact(string name, ArtifactConfiguration configuration)
		=> this.ArtifactManagerProvider().RegisterArtifact(this.ModManifest, name, configuration);
}
