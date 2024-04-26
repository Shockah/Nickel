using Nickel.Models.Content;
using System.Collections.Generic;

namespace Nickel;

public readonly struct GetVolatileCardTraitOverridesEventArgs
{
	public required State State { get; init; }
	public required Card Card { get; init; }
	public required IReadOnlyDictionary<ICardTraitEntry, CardTraitState> TraitStates { get; init; }
	internal Dictionary<ICardTraitEntry, bool?> VolatileOverrides { get; init; }

	public void SetVolatileOverride(ICardTraitEntry trait, bool? overrideValue)
		=> this.VolatileOverrides[trait] = overrideValue;
}
