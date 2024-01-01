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
	PartManager Parts,
	ShipManager Ships
)
{
	public ContentManager(Func<ModLoadPhase> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider) : this(
		new SpriteManager(),
		new DeckManager(currentModLoadPhaseProvider),
		new StatusManager(currentModLoadPhaseProvider),
		new CardManager(currentModLoadPhaseProvider),
		new ArtifactManager(currentModLoadPhaseProvider),
		new CharacterManager(currentModLoadPhaseProvider, loggerProvider),
		new PartManager(currentModLoadPhaseProvider),
		new ShipManager(currentModLoadPhaseProvider)
	)
	{ }

	internal void InjectQueuedEntries()
	{
		this.Decks.InjectQueuedEntries();
		this.Statuses.InjectQueuedEntries();
		this.Cards.InjectQueuedEntries();
		this.Artifacts.InjectQueuedEntries();
		this.Characters.InjectQueuedEntries();
		this.Parts.InjectQueuedEntries();
		this.Ships.InjectQueuedEntries();
	}
}
