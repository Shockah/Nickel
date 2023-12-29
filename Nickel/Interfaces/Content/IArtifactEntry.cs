namespace Nickel;

public interface IArtifactEntry : IModOwned
{
	ArtifactConfiguration Configuration { get; }
}
