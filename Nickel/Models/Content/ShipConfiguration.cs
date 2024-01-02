namespace Nickel;

public readonly struct ShipConfiguration
{
	public StarterShip Ship { get; init; }
	public Spr? UnderChassisSprite { get; init; }
	public Spr? OverChassisSprite { get; init; }
	public bool StartLocked { get; init; }
	public SingleLocalizationProvider? Name { get; init; }
	public SingleLocalizationProvider? Description { get; init; }
}
