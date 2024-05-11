using System;

namespace Nickel;

public readonly struct EnemyConfiguration
{
	public required Type EnemyType { get; init; }
	public required Func<State, MapBase, BattleType?> ShouldAppearOnMap { get; init; }
	public SingleLocalizationProvider? Name { get; init; }
}
