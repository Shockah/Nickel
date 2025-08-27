namespace Nickel;

/// <seealso cref="IModCards.OnSetCardTraitOverride"/>
public readonly struct SetCardTraitOverrideEventArgs
{
	/// <summary>The current state of the game.</summary>
	public required State State { get; init; }
	
	/// <summary>The card a trait is being overridden for.</summary>
	public required Card Card { get; init; }
	
	/// <summary>The card trait being overridden.</summary>
	public required ICardTraitEntry CardTrait { get; init; }
	
	/// <summary>The override value.</summary>
	public required bool? OverrideValue { get; init; }
	
	/// <summary>Whether the override is permanent.</summary>
	public required bool IsPermanent { get; init; }
}
