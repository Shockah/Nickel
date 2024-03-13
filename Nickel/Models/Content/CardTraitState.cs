namespace Nickel.Models.Content;

/// <summary>
/// Describes the full state of a single card trait on a single card.
/// </summary>
/// <param name="Innate">Whether the card has this card trait active innately.</param>
/// <param name="VolatileOverride">A "volatile" override for the trait, done via the <see cref="IModCards.OnGetVolatileCardTraitOverrides"/> event.</param>
/// <param name="PermanentOverride">A permanent override for the trait. This override does not get cleared when combat ends. See <see cref="SafetyLock"/>.</param>
/// <param name="TemporaryOverride">A temporary override for the trait. This override gets cleared when combat ends. See <see cref="RootAccess"/>.</param>
public readonly record struct CardTraitState(
	bool Innate,
	bool? VolatileOverride,
	bool? PermanentOverride,
	bool? TemporaryOverride
)
{
	/// <summary>Whether this card trait is currently active.</summary>
	public bool IsActive
		=> this.TemporaryOverride ?? this.PermanentOverride ?? this.VolatileOverride ?? this.Innate;
}
