using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

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

	public ContentManager(
		SpriteManager sprites,
		DeckManager decks,
		StatusManager statuses,
		CardManager cards,
		ArtifactManager artifacts,
		CharacterManager characters,
		PartManager parts,
		ShipManager ships
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

		StatePatches.OnLoad.Subscribe(this, this.OnLoad);
	}

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

	private void OnLoad(object? _, StatePatches.LoadEventArgs e)
	{
		void RemoveUnknownCards(List<Card> cards)
		{
			for (var i = 0; i < cards.Count; i++)
				if (!DB.cardMetas.TryGetValue(cards[i].Key(), out var meta) || this.Decks.LookupByDeck(meta.deck) is null)
					cards.RemoveAt(i--);
		}

		for (var i = 0; i < e.State.characters.Count; i++)
			if (e.State.characters[i].deckType is { } deck && this.Decks.LookupByDeck(deck) is null)
				e.State.characters.RemoveAt(i--);
		foreach (var selectedChar in e.State.runConfig.selectedChars.ToList())
			if (this.Decks.LookupByDeck(selectedChar) is null)
				e.State.runConfig.selectedChars.Remove(selectedChar);

		RemoveUnknownCards(e.State.deck);
		if (e.State.route is Combat combat)
		{
			RemoveUnknownCards(combat.hand);
			RemoveUnknownCards(combat.discard);
			RemoveUnknownCards(combat.exhausted);
		}
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
