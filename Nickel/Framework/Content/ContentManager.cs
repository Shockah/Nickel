using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ContentManager
{
	public SpriteManager Sprites { get; }
	public DeckManager Decks { get; }
	public StatusManager Statuses { get; }
	public CardManager Cards { get; }
	public ArtifactManager Artifacts { get; }
	public CharacterManager Characters { get; }
	public PartManager Parts { get; }
	public ShipManager Ships { get; }
	public CardTraitManager CardTraits { get; }
	public EnemyManager Enemies { get; }

	private ContentManager(
		SpriteManager sprites,
		DeckManager decks,
		StatusManager statuses,
		CardManager cards,
		ArtifactManager artifacts,
		CharacterManager characters,
		PartManager parts,
		ShipManager ships,
		CardTraitManager cardTraits,
		EnemyManager enemies
	)
	{
		this.Sprites = sprites;
		this.Decks = decks;
		this.Statuses = statuses;
		this.Cards = cards;
		this.Artifacts = artifacts;
		this.Characters = characters;
		this.Parts = parts;
		this.Ships = ships;
		this.CardTraits = cardTraits;
		this.Enemies = enemies;
	}

	public static ContentManager Create(Func<ModLoadPhase> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider, EnumCasePool enumCasePool, IModManifest vanillaModManifest, IModManifest modManagerModManifest, ModDataManager modDataManager)
	{
		var sprites = new SpriteManager(enumCasePool, vanillaModManifest);
		var decks = new DeckManager(currentModLoadPhaseProvider, enumCasePool, vanillaModManifest);
		var statuses = new StatusManager(currentModLoadPhaseProvider, enumCasePool, vanillaModManifest);
		var cards = new CardManager(currentModLoadPhaseProvider, loggerProvider, vanillaModManifest);
		var artifacts = new ArtifactManager(currentModLoadPhaseProvider, loggerProvider, vanillaModManifest);
		var characters = new CharacterManager(currentModLoadPhaseProvider, loggerProvider, sprites, decks, statuses, vanillaModManifest);
		var parts = new PartManager(enumCasePool, currentModLoadPhaseProvider);
		var ships = new ShipManager(currentModLoadPhaseProvider, vanillaModManifest);
		var cardTraits = new CardTraitManager(loggerProvider, vanillaModManifest, modManagerModManifest, modDataManager);
		var enemies = new EnemyManager(currentModLoadPhaseProvider, loggerProvider, vanillaModManifest);
		return new(sprites, decks, statuses, cards, artifacts, characters, parts, ships, cardTraits, enemies);
	}

	internal void InjectQueuedEntries()
	{
		this.Decks.InjectQueuedEntries();
		this.Statuses.InjectQueuedEntries();
		this.Cards.InjectQueuedEntries();
		this.Artifacts.InjectQueuedEntries();
		this.Characters.InjectQueuedEntries();
		this.Parts.InjectQueuedEntries();
		this.Ships.InjectQueuedEntries();
		this.Enemies.InjectQueuedEntries();
	}

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		this.Decks.InjectLocalizations(locale, localizations);
		this.Statuses.InjectLocalizations(locale, localizations);
		this.Cards.InjectLocalizations(locale, localizations);
		this.Artifacts.InjectLocalizations(locale, localizations);
		this.Characters.InjectLocalizations(locale, localizations);
		this.Ships.InjectLocalizations(locale, localizations);
		this.Enemies.InjectLocalizations(locale, localizations);
	}

	internal void ModifyJsonContract(Type type, JsonContract contract)
	{
		this.Decks.ModifyJsonContract(type, contract);
		this.Statuses.ModifyJsonContract(type, contract);
	}
}
