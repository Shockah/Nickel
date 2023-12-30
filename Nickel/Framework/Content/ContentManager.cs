using Microsoft.Extensions.Logging;
using System;

namespace Nickel;

internal sealed record ContentManager(
	SpriteManager Sprites,
	DeckManager Decks,
	StatusManager Statuses,
	CardManager Cards,
	ArtifactManager Artifacts,
	CharacterManager Characters,
	ShipManager Ships,
	PartManager Parts
)
{
	public ContentManager(Func<ModLoadPhase> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider) : this(
		new SpriteManager(),
		new DeckManager(currentModLoadPhaseProvider),
		new StatusManager(currentModLoadPhaseProvider),
		new CardManager(currentModLoadPhaseProvider),
		new ArtifactManager(currentModLoadPhaseProvider),
		new CharacterManager(currentModLoadPhaseProvider, loggerProvider),
		new ShipManager(currentModLoadPhaseProvider),
		new PartManager(currentModLoadPhaseProvider)
	)
	{ }

	internal void InjectQueuedEntries()
	{
		this.Decks.InjectQueuedEntries();
		this.Statuses.InjectQueuedEntries();
		this.Cards.InjectQueuedEntries();
		this.Artifacts.InjectQueuedEntries();
		this.Characters.InjectQueuedEntries();
		this.Ships.InjectQueuedEntries();
		this.Parts.InjectQueuedEntries();
	}
}
