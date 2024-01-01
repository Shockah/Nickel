using System;

namespace Nickel;

public readonly struct CardConfiguration
{
	public Type CardType { get; init; }
	public CardMeta Meta { get; init; }
	public Spr? Art { get; init; }
	public LocalizationProvider? Name { get; init; }
	public LocalizationProvider? Description { get; init; }
	public LocalizationProvider? DescriptionA { get; init; }
	public LocalizationProvider? DescriptionB { get; init; }
}
