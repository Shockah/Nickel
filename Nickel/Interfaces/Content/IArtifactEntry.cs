namespace Nickel;

/// <summary>
/// Describes an <see cref="Artifact"/>.
/// </summary>
public interface IArtifactEntry : IModOwned
{
	/// <summary>The configuration used to register the <see cref="Artifact"/>.</summary>
	ArtifactConfiguration Configuration { get; }
}
