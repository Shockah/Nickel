namespace Nickel;

/// <summary>
/// Describes a ship part type (<see cref="PType"/>).
/// </summary>
public interface IPartTypeEntry : IModOwned
{
	/// <summary>The part type (<see cref="PType"/>) described by this entry.</summary>
	PType PartType { get; }
	
	/// <summary>The configuration used to register the part type (<see cref="PType"/>).</summary>
	PartTypeConfiguration Configuration { get; }
}
