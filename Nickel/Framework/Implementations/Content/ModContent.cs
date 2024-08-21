using Nanoray.ServiceLocator;

namespace Nickel;

internal sealed class ModContent
	: IModContent
{
	[Injectable]
	public IModSprites Sprites { get; }
	
	[Injectable]
	public IModDecks Decks { get; }
	
	[Injectable]
	public IModStatuses Statuses { get; }
	
	[Injectable]
	public IModCards Cards { get; }
	
	[Injectable]
	public IModArtifacts Artifacts { get; }
	
	[Injectable]
	public IModCharacters Characters { get; }
	
	[Injectable]
	public IModShips Ships { get; }
	
	[Injectable]
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
