namespace Nickel;

/// <summary>
/// Describes a custom sound.
/// </summary>
public interface IModSoundEntry : ISoundEntry
{
	/// <summary>
	/// The FMOD sound.
	/// </summary>
	FMOD.Sound Sound { get; }
	
	/// <inheritdoc cref="ISoundEntry.CreateInstance"/>
	new IModSoundInstance CreateInstance(bool started = true);
	
	ISoundInstance ISoundEntry.CreateInstance(bool started)
		=> this.CreateInstance(started);
}
