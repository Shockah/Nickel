using FMOD;

namespace Nickel;

public interface IEventSoundEntry : ISoundEntry
{
	GUID BankId { get; }
	GUID EventId { get; }
	
	IEventSoundInstance CreateInstance(IModAudio helper, bool started = true);
	
	ISoundInstance ISoundEntry.CreateInstance(IModAudio helper, bool started)
		=> this.CreateInstance(helper, started);
}
