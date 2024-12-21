namespace Nickel;

/// <summary>
/// Describes a ship part with a specific skin.
/// </summary>
public interface IPartEntry : IModOwned
{
	/// <summary>The configuration used to register the part.</summary>
	PartConfiguration Configuration { get; }
}
