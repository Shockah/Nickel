namespace Nickel;

/// <summary>
/// Describes a non-playable <see cref="Character"/> - an enemy or a story character.
/// </summary>
public interface INonPlayableCharacterEntryV2 : ICharacterEntryV2
{
	/// <summary>The configuration used to register the non-playable <see cref="Character"/>.</summary>
	NonPlayableCharacterConfigurationV2 Configuration { get; }
}
