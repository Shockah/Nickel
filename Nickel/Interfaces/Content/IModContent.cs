namespace Nickel;

public interface IModContent
{
	IModSprites Sprites { get; }
	IModDecks Decks { get; }
	IModStatuses Statuses { get; }
	IModCards Cards { get; }
	IModArtifacts Artifacts { get; }
	IModCharacters Characters { get; }
	IModShips Ships { get; }
	IModParts Parts { get; }
}
