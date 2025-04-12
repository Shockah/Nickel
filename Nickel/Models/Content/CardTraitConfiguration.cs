using System;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Describes all aspects of a card trait (which usually appears in the bottom-right of a card).
/// </summary>
public readonly struct CardTraitConfiguration
{
	/// <summary>An icon provider for the card trait.</summary>
	public required Func<State, Card?, Spr?> Icon { get; init; }
	
	/// <summary>A custom renderer delegate for the card trait icon.</summary>
	public Func<State, Card?, Vec, bool>? Renderer { get; init; }
	
	/// <summary>A localization provider for the name of the card trait.</summary>
	public SingleLocalizationProvider? Name { get; init; }
	
	/// <summary>A tooltip provider for the card trait.</summary>
	public Func<State, Card?, IEnumerable<Tooltip>>? Tooltips { get; init; }
}
