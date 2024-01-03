namespace Nickel;

public readonly struct DeckConfiguration
{
	public required DeckDef Definition { get; init; }
	public required Spr DefaultCardArt { get; init; }
	public required Spr BorderSprite { get; init; }
	public Spr? OverBordersSprite { get; init; }
	public SingleLocalizationProvider? Name { get; init; }
}
