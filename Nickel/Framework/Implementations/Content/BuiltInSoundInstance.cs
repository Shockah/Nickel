using FMOD.Studio;

namespace Nickel;

internal sealed class BuiltInSoundInstance(BuiltInSoundEntry entry, EventInstance instance) : ISoundInstance
{
	public ISoundEntry Entry { get; } = entry;
	
	private EventInstance Instance = instance;

	public bool IsPaused
	{
		get
		{
			Audio.Catch(this.Instance.getPaused(out var paused));
			return paused;
		}
		set => Audio.Catch(this.Instance.setPaused(value));
	}

	public float Volume
	{
		get
		{
			Audio.Catch(this.Instance.getVolume(out var volume));
			return volume;
		}
		set => Audio.Catch(this.Instance.setVolume(value));
	}

	public float Pitch
	{
		get
		{
			Audio.Catch(this.Instance.getPitch(out var volume));
			return volume;
		}
		set => Audio.Catch(this.Instance.setPitch(value));
	}
}
