using FMOD;
using System;

namespace Nickel;

internal sealed class ModSoundInstance(ModSoundEntry entry, Channel channel, int id) : IModSoundInstance
{
	public IModSoundEntry Entry { get; } = entry;
	public Channel Channel { get; } = channel;

	public override string ToString()
		=> this.Entry.UniqueName;

	public override int GetHashCode()
		=> HashCode.Combine(this.Entry.UniqueName.GetHashCode(), id);

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
