using System;

namespace Nickel;

public readonly struct CardConfiguration
{
	public Type CardType { get; init; }
	public CardMeta Meta { get; init; }
	public Spr? Art { get; init; }
}
