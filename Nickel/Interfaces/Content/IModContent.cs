namespace Nickel;

/// <summary>
/// A mod-specific content registry.
/// Allows looking up and registering game content.
/// </summary>
public interface IModContent
{
	/// <inheritdoc cref="IModSprites"/>
	IModSprites Sprites { get; }
	
	/// <inheritdoc cref="IModAudio"/>
	IModAudio Audio { get; }

	/// <inheritdoc cref="IModDecks"/>
	IModDecks Decks { get; }

	/// <inheritdoc cref="IModStatuses"/>
	IModStatuses Statuses { get; }

	/// <inheritdoc cref="IModCards"/>
	IModCards Cards { get; }

	/// <inheritdoc cref="IModArtifacts"/>
	IModArtifacts Artifacts { get; }

	/// <inheritdoc cref="IModCharacters"/>
	IModCharacters Characters { get; }

	/// <inheritdoc cref="IModShips"/>
	IModShips Ships { get; }
	
	/// <inheritdoc cref="IModEnemies"/>
	IModEnemies Enemies { get; }
}
