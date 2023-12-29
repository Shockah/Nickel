namespace Nickel;

public interface IModArtifacts
{
	IArtifactEntry RegisterArtifact(string name, ArtifactConfiguration configuration);
}
