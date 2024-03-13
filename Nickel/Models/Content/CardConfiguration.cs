using System;

namespace Nickel;

/// <summary>
/// Describes all aspects of a <see cref="Card"/>.
/// </summary>
public readonly struct CardConfiguration
{
	/// <summary>The <see cref="Card"/> subclass.</summary>
	public required Type CardType { get; init; }
	
	/// <summary>The meta information regarding the <see cref="Card"/>.</summary>
	public required CardMeta Meta { get; init; }
	
	/// <summary>The card art to use for this <see cref="Card"/>. If <c>null</c>, the default card art for the <c>Deck</c> will be used instead.</summary>
	public Spr? Art { get; init; }
	
	/// <summary>A localization provider for the name of the <see cref="Card"/>.</summary>
	public SingleLocalizationProvider? Name { get; init; }
}
