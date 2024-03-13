using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Allows attaching custom innate card traits to custom cards.
/// </summary>
/// <remarks>This interface only makes sense to be implemented by <see cref="Card"/> subclasses.</remarks>
public interface IHasCustomCardTraits
{
	/// <summary>
	/// Provides a set of card traits the card should have innately.
	/// </summary>
	/// <param name="state">The current state of the game.</param>
	/// <returns>A set of card traits the card should have innately.</returns>
	IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state);
}
