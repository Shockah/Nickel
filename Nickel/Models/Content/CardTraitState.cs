using System;

namespace Nickel.Models.Content;

/// <summary>
/// Describes the full state of a single card trait on a single card.
/// </summary>
/// <param name="Innate">Whether the card has this card trait active innately.</param>
/// <param name="DynamicInnateOverride">A dynamic override for the trait that <b>does not</b> take <see cref="PermanentOverride"/> or <see cref="TemporaryOverride"/> into account, done via the <see cref="IModCards.OnGetVolatileCardTraitOverrides"/> event.</param>
/// <param name="PermanentOverride">A permanent override for the trait. This override does not get cleared when combat ends. See <see cref="SafetyLock"/>.</param>
/// <param name="TemporaryOverride">A temporary override for the trait. This override gets cleared when combat ends. See <see cref="RootAccess"/>.</param>
/// <param name="FinalDynamicOverride">A dynamic override for the trait that <b>does</b> take <see cref="PermanentOverride"/> and <see cref="TemporaryOverride"/> into account, done via the <see cref="IModCards.OnGetVolatileCardTraitOverrides"/> event.</param>
public readonly record struct CardTraitState(
	bool Innate,
	bool? DynamicInnateOverride,
	bool? PermanentOverride,
	bool? TemporaryOverride,
	bool? FinalDynamicOverride
)
{
	/// <summary>A "volatile" override for the trait, done via the <see cref="IModCards.OnGetVolatileCardTraitOverrides"/> event.</summary>
	[Obsolete($"Use `{nameof(DynamicInnateOverride)}` instead.")]
	public bool? VolatileOverride
		=> this.DynamicInnateOverride;

	/// <summary>Whether this card trait is currently active.</summary>
	public bool IsActive
		=> this.FinalDynamicOverride ?? this.TemporaryOverride ?? this.PermanentOverride ?? this.DynamicInnateOverride ?? this.Innate;
}
