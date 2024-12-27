using FMOD;
using Nanoray.PluginManager;
using System;
using System.IO;

namespace Nickel;

/// <summary>
/// A mod-specific audio registry.
/// Allows looking up and registering sounds.
/// </summary>
public interface IModAudio
{
	/// <summary>
	/// Retrieves an <see cref="ISoundEntry"/> for a given FMOD bank event ID.
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
	/// <param name="name">The local (mod-level) name of the sound. This has to be unique across the mod.</param>
	/// <param name="file">The file to load the audio data from.</param>
	/// <returns>A new sprite entry.</returns>
	IModSoundEntry RegisterSound(string name, IFileInfo file);
	
	/// <summary>
	/// Registers a sound, with audio data coming from a <see cref="Stream"/>.
	/// </summary>
	/// <remarks>
	/// The sound entry will have a random content name.
	/// </remarks>
	/// <param name="streamProvider">A stream provider.</param>
	/// <returns>A new sound entry.</returns>
	IModSoundEntry RegisterSound(Func<Stream> streamProvider);

	/// <summary>
	/// Registers a sound, with audio data coming from a <see cref="Stream"/>.
	/// </summary>
	/// <param name="name">The local (mod-level) name of the sound. This has to be unique across the mod.</param>
	/// <param name="streamProvider">A stream provider.</param>
	/// <returns>A new sound entry.</returns>
	IModSoundEntry RegisterSound(string name, Func<Stream> streamProvider);
	
	/// <summary>
	/// Registers a sound with a custom implementation.
	/// </summary>
	/// <param name="factory">The factory that will create the sound entry.</param>
	/// <param name="name">The local (mod-level) name of the sound. This has to be unique across the mod.</param>
	/// <param name="args">The arguments used to create the sound.</param>
	/// <typeparam name="TEntry">The type of the sound entry.</typeparam>
	/// <typeparam name="TArgs">The type of arguments used to create the sound.</typeparam>
	/// <returns>A new sound entry.</returns>
	TEntry RegisterSound<TEntry, TArgs>(ICustomSoundEntryFactory<TEntry, TArgs> factory, string name, TArgs args) where TEntry : ICustomSoundEntry;
	
	/// <summary>
	/// Registers a sound with a custom implementation.
	/// </summary>
	/// <param name="name">The local (mod-level) name of the sound. This has to be unique across the mod.</param>
	/// <param name="args">The arguments used to create the sound.</param>
	/// <typeparam name="TEntry">The type of the sound entry.</typeparam>
	/// <typeparam name="TArgs">The type of arguments used to create the sound.</typeparam>
	/// <returns>A new sound entry.</returns>
	TEntry RegisterSound<TEntry, TArgs>(string name, TArgs args)
		where TEntry : ICustomSoundEntry
		where TArgs : ICustomSoundEntryArgsWithDefaultFactory<TEntry, TArgs>;
	
	/// <summary>
	/// Registers an FMOD sound event bank from a file.
	/// </summary>
	/// <param name="file">The file to load the bank from.</param>
	void RegisterBank(IFileInfo file);
	
	/// <summary>
	/// Registers an FMOD sound event bank from a <see cref="Stream"/>.
	/// </summary>
	/// <param name="streamProvider">A stream provider.</param>
	void RegisterBank(Func<Stream> streamProvider);

	/// <summary>
	/// Obtains a <see cref="Song"/> for a given FMOD bank event ID, that can be used to play music.
	/// </summary>
	/// <param name="eventId">The ID that is used to play this music.</param>
	/// <returns>The <see cref="Song"/> value that can be used to play this music.</returns>
	Song? ObtainSongForEventId(GUID eventId);

	/// <summary>
	/// Obtains a <see cref="Song"/> for a given FMOD bank event sound entry, that can be used to play music.
	/// </summary>
	/// <param name="entry">The sound entry to use as music.</param>
	/// <returns>The <see cref="Song"/> value that can be used to play this music.</returns>
	Song? ObtainSongForSound(IEventSoundEntry entry);
}
