namespace Nickel;

internal sealed class ModContent
	: IModContent
{
	public IModSprites Sprites { get; }
	public IModDecks Decks { get; }
	public IModStatuses Statuses { get; }
	public IModCards Cards { get; }
	public IModArtifacts Artifacts { get; }
	public IModCharacters Characters { get; }
	public IModShips Ships { get; }
	public IModEnemies Enemies { get; }

	public ModContent(
		IModSprites sprites,
		IModDecks decks,
		IModStatuses statuses,
		IModCards cards,
		IModArtifacts artifacts,
		IModCharacters characters,
		IModShips ships,
		IModEnemies enemies
	)
	{
		this.Sprites = sprites;
		this.Decks = decks;
		this.Statuses = statuses;
		this.Cards = cards;
		this.Artifacts = artifacts;
		this.Characters = characters;
		this.Ships = ships;
		this.Enemies = enemies;
	}
}
