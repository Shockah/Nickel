using FMOD.Studio;

namespace Nickel;

public interface IEventSoundInstance : ISoundInstance
{
	EventInstance Instance { get; }
	
	IEventSoundEntry Entry { get; }

	ISoundEntry ISoundInstance.Entry
		=> this.Entry;
}
