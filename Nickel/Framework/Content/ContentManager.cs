using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ContentManager
{
	public readonly SpriteManager Sprites;
	public readonly AudioManager Audio;
	public readonly DeckManager Decks;
	public readonly StatusManager Statuses;
	public readonly CardManager Cards;
	public readonly ArtifactManager Artifacts;
	public readonly CharacterManager Characters;
	public readonly PartManager Parts;
	public readonly ShipManager Ships;
	public readonly CardTraitManager CardTraits;
	public readonly EnemyManager Enemies;

	private ContentManager(
		SpriteManager sprites,
		AudioManager audio,
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
		this.Audio = audio;
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

	public static ContentManager Create(
		Func<ModLoadPhaseState> currentModLoadPhaseProvider,
		Func<IModManifest, ILogger> loggerProvider,
		ModEventManager eventManager,
		EnumCasePool enumCasePool,
		IModManifest vanillaModManifest,
		IModManifest modLoaderModManifest,
		IModDataHandler modDataHandler
	)
	{
		var sprites = new SpriteManager(loggerProvider, enumCasePool, vanillaModManifest);
		var audio = new AudioManager(loggerProvider, currentModLoadPhaseProvider, enumCasePool, vanillaModManifest, modLoaderModManifest);
		var decks = new DeckManager(currentModLoadPhaseProvider, enumCasePool, vanillaModManifest);
		var statuses = new StatusManager(currentModLoadPhaseProvider, enumCasePool, vanillaModManifest);
		var cards = new CardManager(currentModLoadPhaseProvider, loggerProvider, vanillaModManifest);
		var artifacts = new ArtifactManager(currentModLoadPhaseProvider, loggerProvider, vanillaModManifest);
		var characters = new CharacterManager(currentModLoadPhaseProvider, loggerProvider, eventManager, sprites, audio, decks, statuses, cards, vanillaModManifest, modLoaderModManifest);
		var parts = new PartManager(enumCasePool, currentModLoadPhaseProvider);
		var ships = new ShipManager(currentModLoadPhaseProvider, vanillaModManifest);
		var cardTraits = new CardTraitManager(loggerProvider, vanillaModManifest, modLoaderModManifest, modDataHandler);
		var enemies = new EnemyManager(currentModLoadPhaseProvider, loggerProvider, vanillaModManifest);
		return new(sprites, audio, decks, statuses, cards, artifacts, characters, parts, ships, cardTraits, enemies);
	}

	internal void InjectQueuedEntries()
	{
		this.Audio.InjectQueuedEntries();
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
		this.Parts.InjectLocalizations(locale, localizations);
		this.Ships.InjectLocalizations(locale, localizations);
		this.Enemies.InjectLocalizations(locale, localizations);
	}

	internal void ModifyJsonContract(Type type, JsonContract contract)
	{
		this.Decks.ModifyJsonContract(type, contract);
		this.Statuses.ModifyJsonContract(type, contract);
	}
}
