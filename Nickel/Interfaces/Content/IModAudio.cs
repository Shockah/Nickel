using FMOD;
using Nanoray.PluginManager;

namespace Nickel;

/// <summary>
/// A mod-specific audio registry.
/// Allows looking up and registering sounds.
/// </summary>
public interface IModAudio
{
	/// <summary>
	/// Retrieves an <see cref="ISoundEntry"/> for a given built-in ID.
	/// </summary>
	/// <param name="eventId">The ID that is used to play this sound.</param>
	/// <returns>The related sound entry, or <c>null</c> for an invalid ID.</returns>
	IEventSoundEntry? LookupSoundByEventId(GUID eventId);

	/// <summary>
	/// Retrieves an <see cref="ISoundEntry"/> for a given unique sound name.
	/// </summary>
	/// <param name="uniqueName">
	/// The unique name of the sound.<br/>
	/// See also: <seealso cref="IModOwned.UniqueName"/>
	/// </param>
	/// <returns>The sound entry, or <c>null</c> if it couldn't be found.</returns>
	ISoundEntry? LookupSoundByUniqueName(string uniqueName);

	/// <summary>
	/// Registers a sound, with audio data coming from a file.<br/>
	/// The file's path will be used for the content name.
	/// </summary>
	/// <param name="file">The file to load the audio data from.</param>
	/// <returns>A new sound entry.</returns>
	IModSoundEntry RegisterSound(IFileInfo file);

	/// <summary>
	/// Registers a sound, with audio data coming from a file.<br/>
	/// </summary>
	/// <param name="name">The name for the content.</param>
	/// <param name="file">The file to load the audio data from.</param>
	/// <returns>A new sprite entry.</returns>
	IModSoundEntry RegisterSound(string name, IFileInfo file);

	/// <summary>
	/// Registers a sound with given audio data.
	/// </summary>
	/// <remarks>
	/// The audio entry will have a random content name.
	/// </remarks>
	/// <param name="data">The audio data.</param>
	/// <returns>A new sound entry.</returns>
	IModSoundEntry RegisterSound(byte[] data);

	/// <summary>
	/// Registers a sound with given audio data.
	/// </summary>
	/// <param name="name">The name for the content.</param>
	/// <param name="data">The audio data.</param>
	/// <returns>A new sound entry.</returns>
	IModSoundEntry RegisterSound(string name, byte[] data);
	
	void RegisterBank(byte[] data);

	/// <summary>
	/// Creates a new sound instance (which by default starts playing immediately).
	/// </summary>
	/// <param name="entry">The sound entry to create the instance for.</param>
	/// <param name="started">Whether the sound should play immediately. Defaults to <c>true</c>.</param>
	/// <returns>A sound instance that allows further control over the sound.</returns>
	ISoundInstance CreateInstance(ISoundEntry entry, bool started = true);
}
