using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Nickel.Framework;
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

	public ContentManager(
		SpriteManager sprites,
		DeckManager decks,
		StatusManager statuses,
		CardManager cards,
		ArtifactManager artifacts,
		CharacterManager characters,
		PartManager parts,
		ShipManager ships,
		CardTraitManager cardTraits
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
	}

	public static ContentManager Create(Func<ModLoadPhase> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider, IModManifest vanillaModManifest, IModManifest modManagerModManifest, ModDataManager modDataManager)
	{
		var sprites = new SpriteManager(vanillaModManifest);
		var decks = new DeckManager(currentModLoadPhaseProvider, vanillaModManifest);
		var statuses = new StatusManager(currentModLoadPhaseProvider, vanillaModManifest);
		var cards = new CardManager(currentModLoadPhaseProvider, loggerProvider, vanillaModManifest);
		var artifacts = new ArtifactManager(currentModLoadPhaseProvider, loggerProvider, vanillaModManifest);
		var characters = new CharacterManager(currentModLoadPhaseProvider, loggerProvider, sprites, decks, statuses, vanillaModManifest);
		var parts = new PartManager(currentModLoadPhaseProvider);
		var ships = new ShipManager(currentModLoadPhaseProvider, vanillaModManifest);
		var cardTraits = new CardTraitManager(loggerProvider, vanillaModManifest, modManagerModManifest, modDataManager);
		return new(sprites, decks, statuses, cards, artifacts, characters, parts, ships, cardTraits);
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
	}

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		this.Decks.InjectLocalizations(locale, localizations);
		this.Statuses.InjectLocalizations(locale, localizations);
		this.Cards.InjectLocalizations(locale, localizations);
		this.Artifacts.InjectLocalizations(locale, localizations);
		this.Characters.InjectLocalizations(locale, localizations);
		this.Ships.InjectLocalizations(locale, localizations);
	}

	internal void ModifyJsonContract(Type type, JsonContract contract)
	{
		this.Decks.ModifyJsonContract(type, contract);
		this.Statuses.ModifyJsonContract(type, contract);
	}
}
