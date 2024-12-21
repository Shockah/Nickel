using FMOD;

namespace Nickel;

internal sealed class ModSoundInstance(ModSoundEntry entry, Channel channel) : IModSoundInstance
{
	public IModSoundEntry Entry { get; } = entry;
	public Channel Channel { get; } = channel;

	public bool IsPaused
	{
		get
		{
			Audio.Catch(this.Channel.getPaused(out var paused));
			return paused;
		}
		set => Audio.Catch(this.Channel.setPaused(value));
	}

	public float Volume
	{
		get
		{
			Audio.Catch(this.Channel.getVolume(out var volume));
			return volume;
		}
		set => Audio.Catch(this.Channel.setVolume(value));
	}

	public float Pitch
	{
		get
		{
			Audio.Catch(this.Channel.getPitch(out var volume));
			return volume;
		}
		set => Audio.Catch(this.Channel.setPitch(value));
	}
}
