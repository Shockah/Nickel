namespace Nickel;

/// <summary>
/// Describes a non-playable <see cref="Character"/> - an enemy or a story character.
/// </summary>
public interface INonPlayableCharacterEntry : ICharacterEntry
{
	/// <summary>The configuration used to register the non-playable <see cref="Character"/>.</summary>
	NonPlayableCharacterConfiguration Configuration { get; }

	/// <summary>
	/// Amends a non-playable <see cref="Character"/>'s <see cref="NonPlayableCharacterConfiguration">configuration</see>.
	/// </summary>
	/// <param name="amends">The amends to make.</param>
	/// <remarks>
	/// This method is only valid for modded entries.
	/// </remarks>
	void Amend(NonPlayableCharacterConfiguration.Amends amends);
}
