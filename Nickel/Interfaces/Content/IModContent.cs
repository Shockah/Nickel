namespace Nickel;

/// <summary>
/// A mod-specific content registry.<br/>
/// Allows looking up and registering game content.
/// </summary>
public interface IModContent
{

	/// <summary>
	/// A mod-specific sprite registry.
	/// Allows looking up and registering sprites.
	/// </summary>
	IModSprites Sprites { get; }

	/// <summary>
	/// A mod-specific deck registry.
	/// Allows looking up and registering decks.
	/// </summary>
	IModDecks Decks { get; }

	/// <summary>
	/// A mod-specific status registry.
	/// Allows looking up and registering statuses.
	/// </summary>
	IModStatuses Statuses { get; }

	/// <summary>
	/// A mod-specific card registry.
	/// Allows looking up and registering cards.
	/// </summary>
	IModCards Cards { get; }

	/// <summary>
	/// A mod-specific artifact registry.
	/// Allows looking up and registering artifacts.
	/// </summary>
	IModArtifacts Artifacts { get; }

	/// <summary>
	/// A mod-specific character registry.
	/// Allows looking up and registering characters.
	/// </summary>
	IModCharacters Characters { get; }

	/// <summary>
	/// A mod-specific ship registry.
	/// Allows looking up and registering ships and ship parts.
	/// </summary>
	IModShips Ships { get; }
}
