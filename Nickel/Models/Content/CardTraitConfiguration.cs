using System;

namespace Nickel;

public readonly struct CardTraitConfiguration
{
	public required Func<State, Card, Spr> IconProvider { get; init; }
	public SingleLocalizationProvider? Name { get; init; }
	public Func<State, Card, Tooltip>? TooltipProvider { get; init; }
}
