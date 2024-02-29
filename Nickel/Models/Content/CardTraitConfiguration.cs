using System;

namespace Nickel;

public readonly struct CardTraitConfiguration
{
	public required Spr Icon { get; init; }
	public SingleLocalizationProvider? Name { get; init; }
	public Func<State, Card, Tooltip>? TooltipProvider { get; init; }
}
