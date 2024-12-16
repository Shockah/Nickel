using System;

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
	
	/// <summary>A delegate that can override the default rarity shine of a card.</summary>
	public Func<ShineColorOverrideArgs, Color>? ShineColorOverride { get; init; }

	/// <seealso cref="ShineColorOverride"/>
	public struct ShineColorOverrideArgs
	{
		/// <summary>The current state of the game.</summary>
		public required State State { get; init; }
		
		/// <summary>The card being rendered.</summary>
		public required Card Card { get; init; }
		
		/// <summary>The default color of the rarity shine for this card.</summary>
		public required Color DefaultShineColor { get; init; }
	}
}
