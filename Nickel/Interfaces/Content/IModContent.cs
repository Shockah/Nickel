namespace Nickel;

/// <summary>
/// A mod-specific content registry.<br/>
/// Allows looking up and registering game content.
/// </summary>
public interface IModContent
{
	/// <summary>
	/// A mod-specific <see cref="Spr"/> registry.
	/// Allows looking up and registering sprites.
	/// </summary>
	IModSprites Sprites { get; }

	/// <summary>
	/// A mod-specific <see cref="Deck"/> registry.
	/// Allows looking up and registering decks.
	/// </summary>
	IModDecks Decks { get; }

	/// <summary>
	/// A mod-specific <see cref="Status"/> registry.
	/// Allows looking up and registering statuses.
	/// </summary>
	IModStatuses Statuses { get; }

	/// <summary>
	/// A mod-specific <see cref="Card"/> registry.
	/// Allows looking up and registering cards.
	/// </summary>
	IModCards Cards { get; }

	/// <summary>
	/// A mod-specific <see cref="Artifact"/> registry.
	/// Allows looking up and registering artifacts.
	/// </summary>
	IModArtifacts Artifacts { get; }

	/// <summary>
	/// A mod-specific playable <see cref="Character"/> registry.
	/// Allows looking up and registering characters.
	/// </summary>
	IModCharacters Characters { get; }

	/// <summary>
	/// A mod-specific <see cref="StarterShip"/> registry.
	/// Allows looking up and registering ships and ship parts.
	/// </summary>
	IModShips Ships { get; }
	
	/// <summary>
	/// A mod-specific enemy <see cref="AI"/> registry.
	/// Allows looking up and registering enemies.
	/// </summary>
	IModEnemies Enemies { get; }
}
