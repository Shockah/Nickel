using FMOD;
using FMOD.Studio;
using System;

namespace Nickel;

internal sealed class BuiltInSoundEntry(IModManifest modOwner, string uniqueName, string localName, GUID id) : ISoundEntry
{
	public IModManifest ModOwner { get; } = modOwner;
	public string UniqueName { get; } = uniqueName;
	public string LocalName { get; } = localName;
	private GUID Id { get; } = id;

	public ISoundInstance CreateInstance(IModAudio helper, bool started = true)
	{
		if (Audio.inst is not { } audio)
			throw new NullReferenceException($"{nameof(Audio)}.{nameof(Audio.inst)} is `null`");
		if (audio.PlayEvent(this.Id) is not { } eventInstance)
			throw new NullReferenceException($"{nameof(Audio)}.{nameof(Audio.PlayEvent)} returned `null`");

		if (!started)
			eventInstance.stop(STOP_MODE.IMMEDIATE);
		return new BuiltInSoundInstance(this, eventInstance);
	}
}
