namespace Nickel;

/// <summary>
/// Describes a sound.
/// </summary>
public interface ISoundEntry : IModOwned
{
	/// <summary>The local (mod-level) name of the sound. This has to be unique across the mod. This is usually a file path relative to the mod's package root.</summary>
	string LocalName { get; }

	/// <summary>
	/// Creates a new sound instance for playback purposes.
	/// </summary>
	/// <param name="started">Whether the sound should play immediately. Defaults to <c>true</c>.</param>
	/// <returns>A new sound instance.</returns>
	ISoundInstance CreateInstance(bool started = true);
}
