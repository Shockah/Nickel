using Nickel.Models.Content;
using System.Collections.Generic;

namespace Nickel;

/// <seealso cref="IModCards.OnGetDynamicCardTraitOverrides"/>
public readonly struct GetDynamicInnateCardTraitOverridesEventArgs
{
	/// <summary>The current state of the game.</summary>
	public required State State { get; init; }
	
	/// <summary>The card the card traits are currently being requested for.</summary>
	public required Card Card { get; init; }
	
	/// <summary>The card data at the moment of requesting its card traits.</summary>
	public required CardData CardData { get; init; }
	
	/// <summary>A dictionary containing the state of all known card traits on the card.</summary>
	public required IReadOnlySet<ICardTraitEntry> InnateTraits { get; init; }
	
	/// <summary>A dictionary containing the state of all known card traits on the card.</summary>
	public required IReadOnlyDictionary<ICardTraitEntry, bool> DynamicInnateTraitOverrides { get; init; }
	
	internal Dictionary<ICardTraitEntry, bool?> Overrides { get; init; }

	/// <summary>
	/// Set a dynamic override for the given trait that <b>does not</b> take <see cref="CardTraitState.PermanentOverride"/> or <see cref="CardTraitState.TemporaryOverride"/> into account.
	/// </summary>
	/// <param name="trait">The card trait to override.</param>
	/// <param name="overrideValue">Whether the card trait should be active. A value of <c>null</c> clears the override.</param>
	public void SetOverride(ICardTraitEntry trait, bool? overrideValue)
		=> this.Overrides[trait] = overrideValue;
}
