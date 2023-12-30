namespace Nickel;

public interface IModContent
{
	IModSprites Sprites { get; }
	IModDecks Decks { get; }
	IModStatuses Statuses { get; }
	IModCards Cards { get; }
	IModArtifacts Artifacts { get; }
	IModCharacters Characters { get; }
	IModStarterShips StarterShips { get; }
	IModParts Parts { get; }
}
