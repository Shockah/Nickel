namespace Nickel;

/// <summary>
/// Describes all aspects of a <see cref="Deck"/>.
/// </summary>
public readonly struct DeckConfiguration
{
	/// <summary>The meta information regarding the <see cref="Deck"/>.</summary>
	public required DeckDef Definition { get; init; }
	
	/// <summary>The default card art to use for cards of this <see cref="Deck"/>.</summary>
	public required Spr DefaultCardArt { get; init; }
	
	/// <summary>The card border sprite to use for cards of this <see cref="Deck"/>.</summary>
	public required Spr BorderSprite { get; init; }
	
	/// <summary>An additional sprite to draw over the borders of cards of this <see cref="Deck"/>.</summary>
	public Spr? OverBordersSprite { get; init; }
	
	/// <summary>A localization provider for the name of the <see cref="Deck"/>.</summary>
	public SingleLocalizationProvider? Name { get; init; }
}
