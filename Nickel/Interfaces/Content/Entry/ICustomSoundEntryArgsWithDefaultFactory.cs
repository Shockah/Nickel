namespace Nickel;

/// <summary>
/// Describes arguments needed to create a custom sound, which have an inherent default factory that can be used to create the sound entry.
/// </summary>
/// <typeparam name="TEntry">The type of the sound entry.</typeparam>
/// <typeparam name="TSelf">This type.</typeparam>
public interface ICustomSoundEntryArgsWithDefaultFactory<out TEntry, in TSelf>
	where TEntry : ICustomSoundEntry
	where TSelf : ICustomSoundEntryArgsWithDefaultFactory<TEntry, TSelf>
{
	/// <summary>
	/// The inherent default factory that can be used to create the sound entry.
	/// </summary>
	static abstract ICustomSoundEntryFactory<TEntry, TSelf> DefaultFactory { get; }
}
