namespace Nickel;

/// <summary>
/// Describes a non-playable <see cref="Character"/> - an enemy or a story character.
/// </summary>
public interface INonPlayableCharacterEntryV2 : ICharacterEntryV2
{
	/// <summary>The configuration used to register the non-playable <see cref="Character"/>.</summary>
	NonPlayableCharacterConfigurationV2 Configuration { get; }

	/// <summary>
	/// Amends a non-playable <see cref="Character"/>'s <see cref="NonPlayableCharacterConfigurationV2">configuration</see>.
	/// </summary>
	/// <param name="amends">The amends to make.</param>
	/// <remarks>
	/// This method is only valid for modded entries.
	/// </remarks>
	void Amend(NonPlayableCharacterConfigurationV2.Amends amends);
}
