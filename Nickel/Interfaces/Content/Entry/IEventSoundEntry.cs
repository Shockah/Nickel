using FMOD;

namespace Nickel;

/// <summary>
/// Describes a sound based on an FMOD bank event.
/// </summary>
public interface IEventSoundEntry : ISoundEntry
{
	/// <summary>
	/// The ID of the FMOD bank containing this sound.
	/// </summary>
	GUID BankId { get; }
	
	/// <summary>
	/// The ID of this sound's event definition in the FMOD bank.
	/// </summary>
	GUID EventId { get; }
	
	/// <inheritdoc cref="ISoundEntry.CreateInstance"/>
	new IEventSoundInstance CreateInstance(IModAudio helper, bool started = true);
	
	ISoundInstance ISoundEntry.CreateInstance(IModAudio helper, bool started)
		=> this.CreateInstance(helper, started);
}
