using FMOD.Studio;

namespace Nickel;

/// <summary>
/// Describes a specific instance of playing sound from an FMOD bank event.
/// </summary>
public interface IEventSoundInstance : ISoundInstance
{
	/// <summary>
	/// The FMOD event instance that can be used to control the sound.
	/// </summary>
	EventInstance Instance { get; }
	
	/// <inheritdoc cref="ISoundInstance.Entry"/>
	new IEventSoundEntry Entry { get; }

	ISoundEntry ISoundInstance.Entry
		=> this.Entry;
}
