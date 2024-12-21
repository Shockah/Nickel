namespace Nickel;

public interface IModSoundEntry : ISoundEntry
{
	FMOD.Sound Sound { get; }
	
	IModSoundInstance CreateInstance(IModAudio helper, bool started = true);
	
	ISoundInstance ISoundEntry.CreateInstance(IModAudio helper, bool started)
		=> this.CreateInstance(helper, started);
}
