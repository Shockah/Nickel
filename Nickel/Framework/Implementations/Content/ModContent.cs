namespace Nickel;

internal sealed class ModContent(
	IModSprites sprites,
	IModAudio audio,
	IModDecks decks,
	IModStatuses statuses,
	IModCards cards,
	IModArtifacts artifacts,
	IModCharacters characters,
	IModShips ships,
	IModEnemies enemies
) : IModContent
{
	public IModSprites Sprites { get; } = sprites;
	public IModAudio Audio { get; } = audio;
	public IModDecks Decks { get; } = decks;
	public IModStatuses Statuses { get; } = statuses;
	public IModCards Cards { get; } = cards;
	public IModArtifacts Artifacts { get; } = artifacts;
	public IModCharacters Characters { get; } = characters;
	public IModShips Ships { get; } = ships;
	public IModEnemies Enemies { get; } = enemies;
}
