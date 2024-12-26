namespace Nickel;

/// <summary>
/// Describes the parameters of a <see cref="VariableSoundEntry">variable sound entry</see>.
/// </summary>
public readonly struct VariableSoundEntryConfiguration
{
	/// <summary>
	/// The default configuration of a <see cref="VariableSoundEntry">variable sound entry</see>.
	/// </summary>
	public static VariableSoundEntryConfiguration Default { get; } = new()
	{
		MinVolume = 1,
		MaxVolume = 1,
		MinPitch = 1,
		MaxPitch = 1,
	};
	
	/// <summary>
	/// The minimum volume the sound will play at.
	/// </summary>
	public float MinVolume { get; init; }
	
	/// <summary>
	/// The maximum volume the sound will play at.
	/// </summary>
	public float MaxVolume { get; init; }
	
	/// <summary>
	/// The minimum pitch the sound will play at.
	/// </summary>
	public float MinPitch { get; init; }
	
	/// <summary>
	/// The maximum pitch the sound will play at.
	/// </summary>
	public float MaxPitch { get; init; }
}
