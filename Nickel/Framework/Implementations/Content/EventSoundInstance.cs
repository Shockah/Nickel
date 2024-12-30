using FMOD;
using FMOD.Studio;
using System;

namespace Nickel;

internal sealed class EventSoundInstance(EventSoundEntry entry, EventInstance instance, int id) : IEventSoundInstance
{
	public IEventSoundEntry Entry { get; } = entry;
	public EventInstance Instance { get; } = instance;

	~EventSoundInstance()
	{
		Audio.Catch(this.Instance.getPlaybackState(out var playbackState));
		if (playbackState is PLAYBACK_STATE.STOPPED)
		{
			Audio.Catch(this.Instance.release());
			return;
		}

		this.Instance.setCallback((type, _, _) =>
		{
			if (type is not (EVENT_CALLBACK_TYPE.STOPPED or EVENT_CALLBACK_TYPE.SOUND_STOPPED))
				return RESULT.OK;
			return this.Instance.release();
		});
	}

	public override string ToString()
		=> this.Entry.UniqueName;

	public override int GetHashCode()
		=> HashCode.Combine(this.Entry.UniqueName.GetHashCode(), id);

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
