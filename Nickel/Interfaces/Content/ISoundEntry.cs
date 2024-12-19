namespace Nickel;

/// <summary>
/// Describes a sound.
/// </summary>
public interface ISoundEntry : IModOwned
{
	/// <summary>The local (mod-level) name of the sound. This has to be unique across the mod. This is usually a file path relative to the mod's package root.</summary>
	string LocalName { get; }

	ISoundInstance CreateInstance(IModAudio helper, bool started = true);
}
