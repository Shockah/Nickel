namespace Nickel;

/// <summary>
/// Describes all aspects of a character's dialogue babble.
/// </summary>
public readonly struct CharacterBabbleConfiguration
{
	/// <summary>
	/// The sound the character makes, or <c>null</c> for the default.
	/// </summary>
	public ISoundEntry? Sound { get; init; }
	
	/// <summary>
	/// The period in which the character makes the sound (aka how often it is made).
	/// </summary>
	public double? Period { get; init; }
}
