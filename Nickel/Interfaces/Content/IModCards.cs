using Nickel.Models.Content;
using System;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// A mod-specific card registry.
/// Allows looking up and registering cards.
/// </summary>
public interface IModCards
{
	/// <summary>
	/// Lookup a <see cref="Card"/> entry by its class type.
	/// </summary>
	/// <param name="cardType">The type to retrieve an entry for.</param>
	/// <returns>An entry, or <c>null</c> if the type does not match any known cards.</returns>
	ICardEntry? LookupByCardType(Type cardType);
	
	/// <summary>
	/// Lookup an <see cref="Card"/> entry by its full <see cref="IModOwned.UniqueName"/>.
	/// </summary>
	/// <param name="uniqueName">The unique name to retrieve an entry for.</param>
	/// <returns>An entry, or <c>null</c> if the unique name does not match any known cards.</returns>
	ICardEntry? LookupByUniqueName(string uniqueName);
	
	/// <summary>
	/// Register a new <see cref="Card"/>.
	/// </summary>
	/// <param name="configuration">A configuration describing all aspects of the <see cref="Card"/>.</param>
	/// <returns>An entry for the new <see cref="Card"/>.</returns>
	ICardEntry RegisterCard(CardConfiguration configuration);
	
	/// <summary>
	/// Register a new <see cref="Card"/>.
	/// </summary>
	/// <param name="name">The local (mod-level) name for the <see cref="Card"/>. This has to be unique across all artifacts in the mod.</param>
	/// <param name="configuration">A configuration describing all aspects of the <see cref="Card"/>.</param>
	/// <returns>An entry for the new <see cref="Card"/>.</returns>
	ICardEntry RegisterCard(string name, CardConfiguration configuration);

	/// <summary>An entry describing the vanilla Exhaust card trait.</summary>
	ICardTraitEntry ExhaustCardTrait { get; }
	
	/// <summary>An entry describing the vanilla Retain card trait.</summary>
	ICardTraitEntry RetainCardTrait { get; }
	
	/// <summary>An entry describing the vanilla Recycle card trait.</summary>
	ICardTraitEntry RecycleCardTrait { get; }
	
	/// <summary>An entry describing the vanilla Infinite card trait.</summary>
	ICardTraitEntry InfiniteCardTrait { get; }
	
	/// <summary>An entry describing the vanilla Unplayable card trait.</summary>
	ICardTraitEntry UnplayableCardTrait { get; }
	
	/// <summary>An entry describing the vanilla Temporary card trait.</summary>
	ICardTraitEntry TemporaryCardTrait { get; }
	
	/// <summary>An entry describing the vanilla Buoyant card trait.</summary>
	ICardTraitEntry BuoyantCardTrait { get; }
	
	/// <summary>An entry describing the vanilla Single Use card trait.</summary>
	ICardTraitEntry SingleUseCardTrait { get; }

	/// <summary>
	/// Lookup a card trait entry by its full <see cref="IModOwned.UniqueName"/>.
	/// </summary>
	/// <param name="uniqueName">The unique name to retrieve an entry for.</param>
	/// <returns>An entry, or <c>null</c> if the unique name does not match any known card traits.</returns>
	ICardTraitEntry? LookupTraitByUniqueName(string uniqueName);
	
	/// <summary>
	/// Register a new card trait (which usually appears in the bottom-right of a card).
	/// </summary>
	/// <param name="name">The local (mod-level) name for the card trait. This has to be unique across all card traits in the mod.</param>
	/// <param name="configuration">A configuration describing all aspects of the card trait.</param>
	/// <returns>An entry for the new card trait.</returns>
	ICardTraitEntry RegisterTrait(string name, CardTraitConfiguration configuration);
	
	/// <summary>
	/// Retrieves all active card traits on the given card.
	/// </summary>
	/// <param name="state">The current state of the game.</param>
	/// <param name="card">The card to retrieve active card traits for.</param>
	/// <returns>A set of all active card traits on the given card.</returns>
	IReadOnlySet<ICardTraitEntry> GetActiveCardTraits(State state, Card card);
	
	/// <summary>
	/// Retrieves all card traits and their state on the given card.
	/// </summary>
	/// <param name="state">The current state of the game.</param>
	/// <param name="card">The card to retrieve card traits for.</param>
	/// <returns>A dictionary containing the state of all known card traits on the given card.</returns>
	IReadOnlyDictionary<ICardTraitEntry, CardTraitState> GetAllCardTraits(State state, Card card);
	
	/// <summary>
	/// Checks whether the given card trait is currently active on the given card.
	/// </summary>
	/// <param name="state">The current state of the game.</param>
	/// <param name="card">The card to check.</param>
	/// <param name="trait">The card trait to check.</param>
	/// <returns>Whether the given card trait is currently active on the given card.</returns>
	bool IsCardTraitActive(State state, Card card, ICardTraitEntry trait);
	
	/// <summary>
	/// Retrives the current state of the given card trait on the given card.
	/// </summary>
	/// <param name="state">The current state of the game.</param>
	/// <param name="card">The card to check.</param>
	/// <param name="trait">The card trait to check.</param>
	/// <returns>The current state of the given card trait on the given card.</returns>
	CardTraitState GetCardTraitState(State state, Card card, ICardTraitEntry trait);
	
	/// <summary>
	/// Overrides a given card trait on the given card, permamently or temporarily.
	/// </summary>
	/// <param name="state">The current state of the game.</param>
	/// <param name="card">The card to set the override on.</param>
	/// <param name="trait">The card trait to override.</param>
	/// <param name="overrideValue">Whether the card trait should be active.</param>
	/// <param name="permanent">Whether the override is permanent. Temporary overrides get cleared when combat ends.</param>
	void SetCardTraitOverride(State state, Card card, ICardTraitEntry trait, bool? overrideValue, bool permanent);

	/// <summary>An event fired whenever a card's card trait state is requested. It can be used to set "volatile" overrides on a card, which act almost as if the trait was innate, depending on other traits or other state.</summary>
	event EventHandler<GetVolatileCardTraitOverridesEventArgs> OnGetVolatileCardTraitOverrides;
}
