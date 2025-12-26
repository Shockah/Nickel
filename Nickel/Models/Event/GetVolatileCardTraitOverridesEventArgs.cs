using System.Collections.Generic;

namespace Nickel;

/// <seealso cref="IModCards.OnGetVolatileCardTraitOverrides"/>
public readonly struct GetVolatileCardTraitOverridesEventArgs
{
	/// <summary>The current state of the game.</summary>
	public required State State { get; init; }
	
	/// <summary>The card the card traits are currently being requested for.</summary>
	public required Card Card { get; init; }
	
	/// <summary>A dictionary containing the state of all known card traits on the card.</summary>
	public required IReadOnlyDictionary<ICardTraitEntry, CardTraitState> TraitStates { get; init; }
	
	internal Dictionary<ICardTraitEntry, bool?> VolatileOverrides { get; init; }

	/// <summary>
	/// Set a "volatile" override for the given trait, which acts almost as if the trait was innate.
	/// </summary>
	/// <param name="trait">The card trait to override.</param>
	/// <param name="overrideValue">Whether the card trait should be active. A value of <c>null</c> clears the override.</param>
	public void SetVolatileOverride(ICardTraitEntry trait, bool? overrideValue)
		=> this.VolatileOverrides[trait] = overrideValue;
}
