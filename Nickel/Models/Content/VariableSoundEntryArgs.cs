namespace Nickel;

/// <summary>
/// Describes the parameters of a <see cref="VariableSoundEntry">variable sound entry</see>.
/// </summary>
/// <param name="wrapped">The sound entry wrapped by this entry and used whenever a sound is to be played.</param>
public sealed class VariableSoundEntryArgs(ISoundEntry wrapped)
{
	/// <summary>
	/// The sound entry wrapped by this entry and used whenever a sound is to be played.
	/// </summary>
	public ISoundEntry Wrapped { get; } = wrapped;

	/// <summary>
	/// The minimum volume the sound will play at.
	/// </summary>
	public float MinVolume { get; init; } = 1f;
	
	/// <summary>
	/// The maximum volume the sound will play at.
	/// </summary>
	public float MaxVolume { get; init; } = 1f;
	
	/// <summary>
	/// The minimum pitch the sound will play at.
	/// </summary>
	public float MinPitch { get; init; } = 1f;
	
	/// <summary>
	/// The maximum pitch the sound will play at.
	/// </summary>
	public float MaxPitch { get; init; } = 1f;
}
