using FMOD;

namespace Nickel.ModExtensions;

public static class AudioExtensions
{
	extension(GUID eventId)
	{
		/// <summary>
		/// The entry for this event ID, if it's registered.
		/// </summary>
		public IEventSoundEntry? Entry
			=> ModExtensions.Helper.Content.Audio.LookupSoundByEventId(eventId);
	}
}
