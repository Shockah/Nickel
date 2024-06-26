using System;

namespace Nickel;

/// <summary>
/// Describes all aspects of an enemy <see cref="AI"/>.
/// </summary>
public readonly struct EnemyConfiguration
{
	/// <summary>The enemy <see cref="AI"/> subclass.</summary>
	public required Type EnemyType { get; init; }
	
	/// <summary>A function controlling whether this enemy can appear on the given map.</summary>
	public required Func<State, MapBase, BattleType?> ShouldAppearOnMap { get; init; }
	
	/// <summary>A localization provider for the name of the enemy <see cref="AI"/>.</summary>
	public SingleLocalizationProvider? Name { get; init; }
}
