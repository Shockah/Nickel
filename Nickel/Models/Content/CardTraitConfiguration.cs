using System;

namespace Nickel;

public readonly struct CardTraitConfiguration
{
	public required Func<State, Card, Spr> Icon { get; init; }
	public SingleLocalizationProvider? Name { get; init; }
	public Func<State, Card, Tooltip>? Tooltip { get; init; }
}
