using FMOD;

namespace Nickel;

public interface IModSoundInstance : ISoundInstance
{
	Channel Channel { get; }
	
	IModSoundEntry Entry { get; }

	ISoundEntry ISoundInstance.Entry
		=> this.Entry;
}
