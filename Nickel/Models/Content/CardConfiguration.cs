using System;

namespace Nickel;

public readonly struct CardConfiguration
{
	public required Type CardType { get; init; }
	public required CardMeta Meta { get; init; }
	public Spr? Art { get; init; }
	public SingleLocalizationProvider? Name { get; init; }
}
