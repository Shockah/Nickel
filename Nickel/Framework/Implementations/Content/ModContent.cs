namespace Nickel;

internal sealed class ModContent
	: IModContent
{
	public IModSprites Sprites { get; init; }
	public IModDecks Decks { get; init; }
	public IModStatuses Statuses { get; init; }
	public IModCards Cards { get; init; }
	public IModArtifacts Artifacts { get; init; }
	public IModCharacters Characters { get; init; }
	public IModShips Ships { get; init; }

	public ModContent(
		IModSprites sprites,
		IModDecks decks,
		IModStatuses statuses,
		IModCards cards,
		IModArtifacts artifacts,
		IModCharacters characters,
		IModShips starterShips
	)
	{
		this.Sprites = sprites;
		this.Decks = decks;
		this.Statuses = statuses;
		this.Cards = cards;
		this.Artifacts = artifacts;
		this.Characters = characters;
		this.Ships = starterShips;
	}
}
