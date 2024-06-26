namespace Nickel;

/// <summary>
/// Describes all aspects of a ship part with a specific skin.
/// </summary>
public readonly struct PartConfiguration
{
	/// <summary>The sprite of the ship part.</summary>
	public required Spr Sprite { get; init; }
	
	/// <summary>The sprite of the ship part when it is currently disabled.</summary>
	public Spr? DisabledSprite { get; init; }
}
