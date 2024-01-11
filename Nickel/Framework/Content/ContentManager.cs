using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

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
	public static ContentManager Create(Func<ModLoadPhase> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider, IModManifest vanillaModManifest)
	{
		var sprites = new SpriteManager();
		var decks = new DeckManager(currentModLoadPhaseProvider, vanillaModManifest);
		var statuses = new StatusManager(currentModLoadPhaseProvider, vanillaModManifest);
		var cards = new CardManager(currentModLoadPhaseProvider, vanillaModManifest);
		var artifacts = new ArtifactManager(currentModLoadPhaseProvider, vanillaModManifest);
		var characters = new CharacterManager(currentModLoadPhaseProvider, loggerProvider, sprites, decks, statuses);
		var parts = new PartManager(currentModLoadPhaseProvider);
		var ships = new ShipManager(currentModLoadPhaseProvider);
		return new(sprites, decks, statuses, cards, artifacts, characters, parts, ships);
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
