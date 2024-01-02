using System;

namespace Nickel;

public readonly struct CardConfiguration
{
	public Type CardType { get; init; }
	public CardMeta Meta { get; init; }
	public Spr? Art { get; init; }
	public SingleLocalizationProvider? Name { get; init; }
	public SingleLocalizationProvider? Description { get; init; }
	public SingleLocalizationProvider? DescriptionA { get; init; }
	public SingleLocalizationProvider? DescriptionB { get; init; }
}
