namespace Nickel;

public readonly struct PartConfiguration
{
	public required Spr Sprite { get; init; }
	public Spr? DisabledSprite { get; init; }
}
