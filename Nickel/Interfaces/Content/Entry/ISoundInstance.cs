namespace Nickel;

/// <summary>
/// Describes a specific instance of playing sound.
/// </summary>
public interface ISoundInstance
{
	/// <summary>
	/// The entry that is being played.
	/// </summary>
	ISoundEntry Entry { get; }
	
	/// <summary>
	/// Whether the sound is paused.
	/// </summary>
	bool IsPaused { get; set; }
	
	/// <summary>
	/// The volume of the sound.
	/// </summary>
	float Volume { get; set; }
	
	/// <summary>
	/// The pitch of the sound.
	/// </summary>
	/// <remarks>
	/// To shift the sound by N semitones, use the following formula: <c>Math.Pow(2.0, n / 12.0)</c>.
	/// </remarks>
	float Pitch { get; set; }
}
