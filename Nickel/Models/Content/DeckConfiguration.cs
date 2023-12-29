namespace Nickel;

public readonly struct DeckConfiguration
{
	public DeckDef Definition { get; init; }
	public Spr DefaultCardArt { get; init; }
	public Spr BorderSprite { get; init; }
	public Spr? OverBordersSprite { get; init; }
}
