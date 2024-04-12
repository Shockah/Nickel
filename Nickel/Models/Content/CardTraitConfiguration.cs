using System;
using System.Collections.Generic;

namespace Nickel;

public readonly struct CardTraitConfiguration
{
	public required Func<State, Card, Spr?> Icon { get; init; }
	public SingleLocalizationProvider? Name { get; init; }
	public Func<State, Card, IEnumerable<Tooltip>>? Tooltips { get; init; }
}
