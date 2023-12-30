using Microsoft.Extensions.Logging;
using System;

namespace Nickel;

internal sealed class ContentManager
{
	public SpriteManager Sprites { get; private init; }
	public DeckManager Decks { get; }
	public StatusManager Statuses { get; }
	public CardManager Cards { get; }
	public ArtifactManager Artifacts { get; }
	public CharacterManager Characters { get; }
	public ShipManager Ships { get; }
	public PartManager Parts { get; }

	public ContentManager(
		SpriteManager sprites,
		DeckManager decks,
		StatusManager statuses,
		CardManager cards,
		ArtifactManager artifacts,
		CharacterManager characters,
		ShipManager starterShipManager,
		PartManager parts
	)
	{
		this.Sprites = sprites;
		this.Decks = decks;
		this.Statuses = statuses;
		this.Cards = cards;
		this.Artifacts = artifacts;
		this.Characters = characters;
		this.Ships = starterShipManager;
		this.Parts = parts;
	}

	public ContentManager(
		Func<ModLoadPhase> currentModLoadPhaseProvider,
		Func<IModManifest, ILogger> loggerProvider
	) : this(
		new SpriteManager(),
		new DeckManager(currentModLoadPhaseProvider),
		new StatusManager(currentModLoadPhaseProvider),
		new CardManager(currentModLoadPhaseProvider),
		new ArtifactManager(currentModLoadPhaseProvider),
		new CharacterManager(currentModLoadPhaseProvider, loggerProvider),
		new ShipManager(currentModLoadPhaseProvider),
		new PartManager(currentModLoadPhaseProvider)
	)
	{
	}

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
