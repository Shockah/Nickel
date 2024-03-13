using System;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Describes all aspects of a <see cref="StarterShip"/>.
/// </summary>
public readonly struct ShipConfiguration
{
	/// <summary>The actual <see cref="StarterShip"/> definition.</summary>
	public required StarterShip Ship { get; init; }
	
	/// <summary>The ship sprite drawn on the bottom layer. Use it instead of setting <see cref="Ship.chassisUnder"/> directly.</summary>
	public Spr? UnderChassisSprite { get; init; }
	
	/// <summary>The ship sprite drawn on the top layer. Use it instead of setting <see cref="Ship.chassisOver"/> directly.</summary>
	public Spr? OverChassisSprite { get; init; }
	
	/// <summary>Whether the ship should start locked.</summary>
	public bool StartLocked { get; init; }
	
	/// <summary>The artifact types exclusive to this ship.</summary>
	public IReadOnlySet<Type>? ExclusiveArtifactTypes { get; init; }
	
	/// <summary>A localization provider for the name of the <see cref="StarterShip"/>.</summary>
	public SingleLocalizationProvider? Name { get; init; }
	
	/// <summary>A localization provider for the description of the <see cref="StarterShip"/>.</summary>
	public SingleLocalizationProvider? Description { get; init; }
}
