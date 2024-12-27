namespace Nickel;

/// <summary>
/// Represents a factory that can create sound entries with a custom implementation.
/// </summary>
/// <typeparam name="TEntry">The type of the sound entry.</typeparam>
/// <typeparam name="TArgs">The type of arguments used to create the sound.</typeparam>
public interface ICustomSoundEntryFactory<out TEntry, in TArgs> where TEntry : ICustomSoundEntry
{
	/// <summary>
	/// Provides the <see cref="IModOwned.UniqueName"/> for a new custom sound entry.
	/// </summary>
	/// <param name="owner">The mod that owns this content.</param>
	/// <param name="localName">The local (mod-level) name of the sound. This has to be unique across the mod.</param>
	/// <param name="args">The arguments used to create the sound.</param>
	/// <returns>The unique name for the new custom sound entry.</returns>
	string GetUniqueName(IModManifest owner, string localName, TArgs args);
	
	/// <summary>
	/// Creates a new sound entry.
	/// </summary>
	/// <param name="owner">The mod that owns this content.</param>
	/// <param name="uniqueName">The unique name for the new custom sound entry, returned earlier via <see cref="GetUniqueName"/>.</param>
	/// <param name="localName">The local (mod-level) name of the sound. This has to be unique across the mod.</param>
	/// <param name="args">The arguments used to create the sound.</param>
	/// <returns>A new sound entry.</returns>
	TEntry CreateEntry(IModManifest owner, string uniqueName, string localName, TArgs args);
}
