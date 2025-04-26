namespace Nickel;

/// <summary>
/// Describes an <see cref="Artifact"/>.
/// </summary>
public interface IArtifactEntry : IModOwned
{
	/// <summary>The configuration used to register the <see cref="Artifact"/>.</summary>
	ArtifactConfiguration Configuration { get; }

	/// <summary>
	/// Amends an <see cref="Artifact"/>'s <see cref="ArtifactConfiguration">configuration</see>.
	/// </summary>
	/// <param name="amends">The amends to make.</param>
	/// <remarks>
	/// This method is only valid for modded entries.
	/// </remarks>
	void Amend(ArtifactConfiguration.Amends amends);
}
