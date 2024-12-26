using FMOD;
using FMOD.Studio;
using System;

namespace Nickel;

internal sealed class EventSoundEntry(IModManifest modOwner, string uniqueName, string localName, GUID bankId, GUID eventId) : IEventSoundEntry
{
	public IModManifest ModOwner { get; } = modOwner;
	public string UniqueName { get; } = uniqueName;
	public string LocalName { get; } = localName;
	public GUID BankId { get; } = bankId;
	public GUID EventId { get; } = eventId;

	public IEventSoundInstance CreateInstance(bool started = true)
	{
		if (Audio.inst is not { } audio)
			throw new NullReferenceException($"{nameof(Audio)}.{nameof(Audio.inst)} is `null`");
		if (audio.PlayEvent(this.EventId) is not { } eventInstance)
			throw new NullReferenceException($"{nameof(Audio)}.{nameof(Audio.PlayEvent)} returned `null`");

		if (!started)
			eventInstance.stop(STOP_MODE.IMMEDIATE);
		return new EventSoundInstance(this, eventInstance);
	}
}
