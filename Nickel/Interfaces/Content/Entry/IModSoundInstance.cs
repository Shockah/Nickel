using FMOD;

namespace Nickel;

/// <summary>
/// Describes a specific instance of a playing custom sound.
/// </summary>
public interface IModSoundInstance : ISoundInstance
{
	/// <summary>
	/// The channel that can be used to control the sound.
	/// </summary>
	Channel Channel { get; }
	
	/// <inheritdoc cref="ISoundInstance.Entry"/>
	new IModSoundEntry Entry { get; }

	ISoundEntry ISoundInstance.Entry
		=> this.Entry;
}
