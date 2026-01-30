using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Nickel;

internal sealed class CharacterManager
{
	private readonly Func<IModManifest, ILogger> LoggerProvider;
	private readonly SpriteManager Sprites;
	private readonly AudioManager Audio;
	private readonly DeckManager Decks;
	private readonly StatusManager Statuses;
	private readonly CardManager Cards;
	private readonly IModManifest VanillaModManifest;
	private readonly AfterDbInitManager<AnimationEntry> AnimationManager;
	private readonly AfterDbInitManager<PlayableCharacterEntry> PlayableCharacterManager;
	private readonly AfterDbInitManager<NonPlayableCharacterEntry> NonPlayableCharacterManager;
	private readonly Dictionary<string, AnimationEntry> UniqueNameToAnimationEntry = [];
	private readonly Dictionary<string, PlayableCharacterEntry> UniqueNameToPlayableCharacterEntry = [];
	private readonly Dictionary<string, NonPlayableCharacterEntry> UniqueNameToNonPlayableCharacterEntry = [];
	private readonly Dictionary<Deck, PlayableCharacterEntry> DeckToCharacterEntry = [];
	private readonly Dictionary<string, ICharacterEntry> CharacterTypeToCharacterEntry = [];

	internal readonly Lazy<List<Deck>> VanillaPlayableCharacterDecks;
	private readonly Lazy<List<string>> VanillaPlayableCharacterDeckNames;
	
	private bool IsDeckOrderUpdateQueued;

	public CharacterManager(
		Func<ModLoadPhaseState> currentModLoadPhaseProvider,
		Func<IModManifest, ILogger> loggerProvider,
		ModEventManager eventManager,
		SpriteManager sprites,
		AudioManager audio,
		DeckManager decks,
		StatusManager statuses,
		CardManager cards,
		IModManifest vanillaModManifest,
		IModManifest modLoaderModManifest
	)
	{
		this.LoggerProvider = loggerProvider;
		this.Sprites = sprites;
		this.Audio = audio;
		this.Decks = decks;
		this.Statuses = statuses;
		this.Cards = cards;
		this.VanillaModManifest = vanillaModManifest;
		this.AnimationManager = new(currentModLoadPhaseProvider, Inject);
		this.PlayableCharacterManager = new(currentModLoadPhaseProvider, this.Inject);
		this.NonPlayableCharacterManager = new(currentModLoadPhaseProvider, this.Inject);

		eventManager.OnModLoadPhaseFinishedEvent.Add(this.OnModLoadPhaseFinished, modLoaderModManifest);
		EventsPatches.OnCrystallizedFriendEvent += OnCrystallizedFriendEvent;
		ShoutPatches.OnModifyBabblePeriod += this.OnModifyBabblePeriod;
		ShoutPatches.OnModifyBabbleSound += this.OnModifyBabbleSound;
		StoryVarsPatches.OnGetUnlockedChars += this.OnGetUnlockedChars;

		var vanillaDeckDefs = new Lazy<List<DeckDef>>(
			() => DB.decks.Values?.ToList() ?? DeckDef.CreateDeckDefs()
		);
		this.VanillaPlayableCharacterDecks = new(
			() => vanillaDeckDefs.Value
				.Where(def => def.canStartRunWith)
				.Select(def => def.deck)
				.ToList()
		);
		this.VanillaPlayableCharacterDeckNames = new(
			() => this.VanillaPlayableCharacterDecks.Value
				.Select(deck => deck.Key())
				.ToList()
		);
	}

	internal void InjectQueuedEntries()
	{
		this.AnimationManager.InjectQueuedEntries();
		this.PlayableCharacterManager.InjectQueuedEntries();
	}

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		foreach (var entry in this.UniqueNameToPlayableCharacterEntry.Values)
			this.InjectLocalization(locale, localizations, entry);
		foreach (var entry in this.UniqueNameToNonPlayableCharacterEntry.Values)
			this.InjectLocalization(locale, localizations, entry);
	}
	
	private PlayableCharacterEntry CreateForVanilla(Deck deck)
	{
		var key = deck.Key();
		var alias = Character.GetSpriteAliasIfExists(key);
		var borderSprite = DB.charPanels[key];
		var def = DB.decks[deck];
		var startLocked = deck is Deck.goat or Deck.eunice or Deck.hacker or Deck.shard or Deck.colorless;
		
		var result = new PlayableCharacterEntry(
			modOwner: this.VanillaModManifest,
			uniqueName: key,
			configuration: new()
			{
				Deck = deck,
				BorderSprite = borderSprite,
				StarterArtifacts = def.starterArtifacts,
				StarterCards = def.starterCards,
				SoloStarterCards = def.starterSoloCards,
				AdjustedMindsetGuaranteedStarterCards = def.starterAMDailyGuaranteedCards,
				StarterCardsFunction = def.starterCardsFunction,
				NeutralAnimation = new()
				{
					CharacterType = alias,
					LoopTag = "neutral",
					Frames = DB.charAnimations[alias]["neutral"],
				},
				MiniAnimation = new()
				{
					CharacterType = alias,
					LoopTag = "mini",
					Frames = DB.charAnimations[alias]["mini"],
				},
				StartLocked = startLocked,
				MissingStatus = new()
				{
					Color = DB.statuses[StatusMeta.deckToMissingStatus[deck]].color,
					Sprite = DB.statuses[StatusMeta.deckToMissingStatus[deck]].icon
				},
				ExeCard = def.exeCard,
				Description = _ => Loc.T($"char.{deck}.desc"),
				Babble = new()
				{
					Sound = this.Audio.LookupSoundByEventId(Shout.GetCharBabble(alias)),
					Period = Shout.BABBLE_INTERVAL_LETTERS,
				},
			},
			missingStatus: this.Statuses.LookupByStatus(StatusMeta.deckToMissingStatus[deck])!,
			amendDelegate: (_, _) => throw new InvalidOperationException("Vanilla entries cannot be amended")
		);
		this.UniqueNameToPlayableCharacterEntry[key] = result;
		this.CharacterTypeToCharacterEntry[result.CharacterType] = result;
		return result;
	}

	private IStatusEntry RegisterMissingStatus(
		IModManifest owner,
		string name,
		IDeckEntry deckEntry,
		Color? configurationColor,
		Spr? configurationSprite
	)
	{
		var color = configurationColor ?? deckEntry.Configuration.Definition.color;
		var sprite = configurationSprite ?? this.Sprites.RegisterSprite(owner, $"{name}::Icon", () =>
		{
			var questionMarkTexture = SpriteLoader.Get(Enum.Parse<Spr>("icons_questionMark"))!;
			var data = new MGColor[questionMarkTexture.Width * questionMarkTexture.Height];
			questionMarkTexture.GetData(data);

			for (var i = 0; i < data.Length; i++)
				data[i] = new MGColor(
					(float)(data[i].R / 255.0 * color.r),
					(float)(data[i].G / 255.0 * color.g),
					(float)(data[i].B / 255.0 * color.b),
					(float)(data[i].A / 255.0 * color.a)
				);

			var texture = new Texture2D(MG.inst.GraphicsDevice, questionMarkTexture.Width, questionMarkTexture.Height);
			texture.SetData(data);
			return texture;
		}, isDynamic: false).Sprite;

		return this.Statuses.RegisterStatus(owner, name, new()
		{
			Definition = new()
			{
				color = color,
				icon = sprite,
				isGood = false
			},
			Name = locale => $"{deckEntry.Configuration.Name?.Invoke(locale)} is missing", // TODO: localize
			Description = locale => $"The next {{0}} <c={color}>{deckEntry.Configuration.Name?.Invoke(locale)}</c> cards you play do nothing.", // TODO: localize
		});
	}

	public ICharacterAnimationEntry RegisterCharacterAnimation(IModManifest owner, string name, CharacterAnimationConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToAnimationEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A character animation with the unique name `{uniqueName}` is already registered", nameof(name));
		var entry = new AnimationEntry(owner, uniqueName, configuration);
		this.UniqueNameToAnimationEntry[entry.UniqueName] = entry;
		this.AnimationManager.QueueOrInject(entry);
		return entry;
	}

	public IPlayableCharacterEntry? LookupByDeck(Deck deck)
	{
		if (this.DeckToCharacterEntry.TryGetValue(deck, out var entry))
			return entry;
		if (Enum.GetValues<Deck>().Contains(deck) && NewRunOptions.allChars.Contains(deck))
			return this.CreateForVanilla(deck);
		return null;
	}

	public ICharacterEntry? LookupByCharacterType(string characterType)
	{
		if (this.CharacterTypeToCharacterEntry.TryGetValue(characterType, out var entry))
			return entry;
		
		switch (characterType)
		{
			case "dizzy":
				return this.LookupByDeck(Deck.dizzy);
			case "riggs":
				return this.LookupByDeck(Deck.riggs);
			case "peri":
				return this.LookupByDeck(Deck.peri);
			case "goat":
				return this.LookupByDeck(Deck.goat);
			case "eunice":
				return this.LookupByDeck(Deck.eunice);
			case "hacker":
				return this.LookupByDeck(Deck.hacker);
			case "shard":
				return this.LookupByDeck(Deck.shard);
			case "comp":
				return this.LookupByDeck(Deck.colorless);
			default:
				if (!DB.currentLocale.strings.ContainsKey($"char.{characterType}"))
					return null;
				return new NonPlayableCharacterEntry(
					this.VanillaModManifest,
					characterType,
					characterType,
					new()
					{
						CharacterType = characterType,
						BorderSprite = DB.charPanels.TryGetValue(characterType, out var borderSprite) ? borderSprite : null,
						NeutralAnimation = new()
						{
							CharacterType = characterType,
							LoopTag = "neutral",
							Frames = DB.charAnimations[Character.GetSpriteAliasIfExists(characterType)]["neutral"]
						},
						Name = _ => Loc.T($"char.{characterType}")
					},
					amendDelegate: (_, _) => throw new InvalidOperationException("Vanilla entries cannot be amended")
				);
		}
	}

	public ICharacterEntry? LookupByUniqueName(string uniqueName)
	{
		if (this.UniqueNameToPlayableCharacterEntry.TryGetValue(uniqueName, out var entry))
			return entry;

		if (this.VanillaPlayableCharacterDeckNames.Value.Contains(uniqueName))
		{
			var deck = Enum.Parse<Deck>(uniqueName);
			return this.CreateForVanilla(deck);
		}

		return null;
	}
	
	public PlayableCharacterEntry RegisterPlayableCharacter(IModManifest owner, string localName, PlayableCharacterConfiguration configuration)
	{
		if (this.Decks.LookupByDeck(configuration.Deck) is not { } deckEntry)
			throw new ArgumentException("Invalid character `Deck`");
		
		var uniqueName = $"{owner.UniqueName}::{localName}";
		if (this.UniqueNameToPlayableCharacterEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A character with the unique name `{uniqueName}` is already registered", nameof(localName));
		
		if (configuration.ExeCard is { } exeCard && this.Cards.LookupByCardType(exeCard.GetType()) is { } exeCardEntry && exeCardEntry.Configuration.Meta.deck != Deck.colorless)
			this.LoggerProvider(owner).LogWarning(
				"Registering a playable character `{Character}` with an EXE card `{Card}` for deck `{Deck}`, but EXE cards should use the `{CatDeck}` deck instead.",
				uniqueName, exeCardEntry.UniqueName, exeCardEntry.Configuration.Meta.deck.Key(), Deck.colorless.Key()
			);
		
		var missingStatus = this.RegisterMissingStatus(owner, $"{localName}::MissingStatus", deckEntry, configuration.MissingStatus.Color, configuration.MissingStatus.Sprite);
		var entry = new PlayableCharacterEntry(owner, uniqueName, configuration, missingStatus, this.Amend);
		this.UniqueNameToPlayableCharacterEntry[entry.UniqueName] = entry;
		this.DeckToCharacterEntry[entry.Configuration.Deck] = entry;
		this.CharacterTypeToCharacterEntry[entry.CharacterType] = entry;
		this.PlayableCharacterManager.QueueOrInject(entry);
		return entry;
	}
	
	public INonPlayableCharacterEntry RegisterNonPlayableCharacter(IModManifest owner, string name, NonPlayableCharacterConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToPlayableCharacterEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A character with the unique name `{uniqueName}` is already registered", nameof(name));
		
		var characterType = $"{owner.UniqueName}::{configuration.CharacterType}";
		var entry = new NonPlayableCharacterEntry(owner, uniqueName, characterType, configuration, this.Amend);
		this.UniqueNameToNonPlayableCharacterEntry[entry.UniqueName] = entry;
		this.CharacterTypeToCharacterEntry[entry.CharacterType] = entry;
		this.NonPlayableCharacterManager.QueueOrInject(entry);
		return entry;
	}

	public bool TryGetCharacterAnimationByUniqueName(string uniqueName, [MaybeNullWhen(false)] out ICharacterAnimationEntry entry)
	{
		if (this.UniqueNameToAnimationEntry.TryGetValue(uniqueName, out var typedEntry))
		{
			entry = typedEntry;
			return true;
		}
		else
		{
			entry = null;
			return false;
		}
	}

	public bool TryGetCharacterByUniqueName(string uniqueName, [MaybeNullWhen(false)] out ICharacterEntry entry)
	{
		if (this.UniqueNameToPlayableCharacterEntry.TryGetValue(uniqueName, out var typedEntry))
		{
			entry = typedEntry;
			return true;
		}
		else
		{
			entry = null;
			return false;
		}
	}
	
	private void QueueDeckOrderUpdate()
	{
		if (this.IsDeckOrderUpdateQueued)
			return;
		
		if (this.PlayableCharacterManager.HasQueuedEntries)
		{
			this.IsDeckOrderUpdateQueued = true;
			return;
		}
		
		this.UpdateDeckOrder();
	}

	private void UpdateDeckOrder()
	{
		this.Decks.QueueDeckOrderUpdate();
		NewRunOptions.allChars = NewRunOptions.GetAvailableChars();
	}

	private static void Inject(AnimationEntry entry)
	{
		ref var characterAnimations = ref CollectionsMarshal.GetValueRefOrAddDefault(DB.charAnimations, entry.Configuration.CharacterType, out var characterAnimationsExists);
		if (!characterAnimationsExists)
			characterAnimations = [];
		characterAnimations![entry.Configuration.LoopTag] = entry.Configuration.Frames.ToList();
	}

	private void Inject(PlayableCharacterEntry entry)
	{
		if (entry.Configuration.NeutralAnimation is { } neutralAnimationConfiguration)
		{
			if (neutralAnimationConfiguration.LoopTag != "neutral")
			{
				this.LoggerProvider(entry.ModOwner).LogError($"Could not inject character {{Character}}: `{nameof(PlayableCharacterConfiguration.NeutralAnimation)}` is not tagged `neutral`.", entry.UniqueName);
				return;
			}
			this.RegisterCharacterAnimation(entry.ModOwner, $"{entry.UniqueName}::neutral", neutralAnimationConfiguration);
		}
		if (entry.Configuration.MiniAnimation is { } miniAnimationConfiguration)
		{
			if (miniAnimationConfiguration.LoopTag != "mini")
			{
				this.LoggerProvider(entry.ModOwner).LogError($"Could not inject character {{Character}}: `{nameof(PlayableCharacterConfiguration.MiniAnimation)}` is not tagged `mini`.", entry.UniqueName);
				return;
			}
			this.RegisterCharacterAnimation(entry.ModOwner, $"{entry.UniqueName}::mini", miniAnimationConfiguration);
		}

		DB.charPanels[entry.CharacterType] = entry.Configuration.BorderSprite;

		if (!DB.decks.TryGetValue(entry.Configuration.Deck, out var deckDef))
			throw new ArgumentException("Invalid character `Deck`");

		deckDef.canStartRunWith = true;
		deckDef.starterArtifacts = entry.Configuration.StarterArtifacts ?? [];
		deckDef.starterCards = entry.Configuration.StarterCards ?? [];
		deckDef.starterSoloCards = entry.Configuration.SoloStarterCards ?? [];
		deckDef.starterAMDailyGuaranteedCards = entry.Configuration.AdjustedMindsetGuaranteedStarterCards ?? [];
		deckDef.starterCardsFunction = entry.Configuration.StarterCardsFunction;
		
		StatusMeta.deckToMissingStatus[entry.Configuration.Deck] = entry.MissingStatus.Status;

		this.InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);

		entry.IsInjected = true;
		
		this.QueueDeckOrderUpdate();
	}
	
	private void Amend(PlayableCharacterEntry entry, PlayableCharacterConfiguration.Amends amends)
	{
		if (!this.UniqueNameToPlayableCharacterEntry.ContainsKey(entry.UniqueName))
			throw new ArgumentException($"A character with the unique name `{entry.UniqueName}` is not registered");

		var def = entry.IsInjected ? DB.decks.GetValueOrDefault(entry.Configuration.Deck) : null;

		if (amends.StarterArtifacts is { } starterArtifacts)
		{
			entry.Configuration = entry.Configuration with { StarterArtifacts = starterArtifacts.Value };
			def?.starterArtifacts = starterArtifacts.Value ?? [];
		}
		if (amends.StarterCards is { } starterCards)
		{
			entry.Configuration = entry.Configuration with { StarterCards = starterCards.Value };
			def?.starterCards = starterCards.Value ?? [];
		}
		if (amends.SoloStarterCards is { } soloStarterCards)
		{
			entry.Configuration = entry.Configuration with { SoloStarterCards = soloStarterCards.Value };
			def?.starterSoloCards = soloStarterCards.Value is null || soloStarterCards.Value.Count == 0
				? CreateDefaultSoloStarters(entry)
				: soloStarterCards.Value;
		}
		if (amends.AdjustedMindsetGuaranteedStarterCards is { } adjustedMindsetGuaranteedStarterCards)
		{
			entry.Configuration = entry.Configuration with { AdjustedMindsetGuaranteedStarterCards = adjustedMindsetGuaranteedStarterCards.Value };
			def?.starterAMDailyGuaranteedCards = adjustedMindsetGuaranteedStarterCards.Value ?? [];
		}
		if (amends.StarterCardsFunction is { } starterCardsFunction)
		{
			entry.Configuration = entry.Configuration with { StarterCardsFunction = starterCardsFunction.Value };
			def?.starterCardsFunction = starterCardsFunction.Value;
		}
		if (amends.ExeCard is { } exeCard)
		{
			entry.Configuration = entry.Configuration with { ExeCard = exeCard.Value };
			def?.exeCard = exeCard.Value;
		}
		
		if (amends.Babble is { } babble)
			entry.Configuration = entry.Configuration with { Babble = babble.Value };
	}
	
	private void Inject(NonPlayableCharacterEntry entry)
	{
		if (entry.Configuration.NeutralAnimation is { } neutralAnimationConfiguration)
		{
			if (neutralAnimationConfiguration.LoopTag != "neutral")
			{
				this.LoggerProvider(entry.ModOwner).LogError($"Could not inject character {{Character}}: `{nameof(NonPlayableCharacterConfiguration.NeutralAnimation)}` is not tagged `neutral`.", entry.UniqueName);
				return;
			}
			this.RegisterCharacterAnimation(entry.ModOwner, $"{entry.UniqueName}::neutral", neutralAnimationConfiguration);
		}

		if (entry.Configuration.BorderSprite is { } borderSprite)
			DB.charPanels[entry.CharacterType] = borderSprite;

		this.InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}
	
	private void Amend(NonPlayableCharacterEntry entry, NonPlayableCharacterConfiguration.Amends amends)
	{
		if (!this.UniqueNameToNonPlayableCharacterEntry.ContainsKey(entry.UniqueName))
			throw new ArgumentException($"A character with the unique name `{entry.UniqueName}` is not registered");

		if (amends.Babble is { } babble)
			entry.Configuration = entry.Configuration with { Babble = babble.Value };
	}

	private void InjectLocalization(string locale, Dictionary<string, string> localizations, PlayableCharacterEntry entry)
	{
		if (entry.ModOwner == this.VanillaModManifest)
			return;
		if (entry.Configuration.Description.Localize(locale) is { } description)
		{
			localizations[$"char.{entry.Configuration.Deck.Key()}.desc"] = description;
			localizations[$"char.{entry.Configuration.Deck}.desc"] = description;
		}
		if (this.Decks.LookupByDeck(entry.Configuration.Deck) is { } deckEntry)
		{
			var characterName = deckEntry.Configuration.Name?.Invoke(locale);
			localizations[$"char.{entry.Configuration.Deck}.desc.missing"] = $"<c={deckEntry.Configuration.Definition.color}>{characterName?.ToUpper()}..?</c>\n{characterName} is missing.";
		}
	}

	private void InjectLocalization(string locale, Dictionary<string, string> localizations, NonPlayableCharacterEntry entry)
	{
		if (entry.ModOwner == this.VanillaModManifest)
			return;
		if (entry.Configuration.Name.Localize(locale) is not { } name)
			return;
		localizations[$"char.{entry.CharacterType}"] = name;
		localizations[$"char.{entry.CharacterType}.name"] = name;
	}

	private void Validate()
	{
		foreach (var entry in this.UniqueNameToPlayableCharacterEntry.Values)
		{
			if (!DB.charAnimations.TryGetValue(entry.CharacterType, out var animations))
			{
				this.LoggerProvider(entry.ModOwner).LogError("Validation error for character `{Character}`: no animations are registered.", entry.UniqueName);
				continue;
			}
			
			if (!animations.ContainsKey("neutral"))
				this.LoggerProvider(entry.ModOwner).LogError("Validation error for character `{Character}`: the `neutral` animation is not registered.", entry.UniqueName);
			if (!animations.ContainsKey("mini"))
				this.LoggerProvider(entry.ModOwner).LogError("Validation error for character `{Character}`: the `mini` animation is not registered.", entry.UniqueName);
			if (!animations.ContainsKey("squint"))
				this.LoggerProvider(entry.ModOwner).LogWarning("Validation warning for character `{Character}`: the `squint` animation is not registered.", entry.UniqueName);
			if (!animations.ContainsKey("gameover"))
				this.LoggerProvider(entry.ModOwner).LogWarning("Validation warning for character `{Character}`: the `gameover` animation is not registered.", entry.UniqueName);
		}
		
		foreach (var entry in this.UniqueNameToNonPlayableCharacterEntry.Values)
		{
			if (!DB.charAnimations.TryGetValue(entry.CharacterType, out var animations))
			{
				this.LoggerProvider(entry.ModOwner).LogError("Validation error for character `{Character}`: no animations are registered.", entry.UniqueName);
				continue;
			}
			
			if (!animations.ContainsKey("neutral"))
				this.LoggerProvider(entry.ModOwner).LogError("Validation error for character `{Character}`: the `neutral` animation is not registered.", entry.UniqueName);
		}
	}

	[EventPriority(double.MaxValue)]
	private void OnModLoadPhaseFinished(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterDbInit)
			return;
		this.SetupAfterDbInit();
		this.Validate();
	}

	private static List<Card> CreateDefaultSoloStarters(PlayableCharacterEntry entry)
	{
		var random = new Rand((uint)GetStableHashCode(entry.UniqueName));

		return [
			.. entry.Configuration.StarterCards ?? [],
			.. DB.cardMetas
				.Where(kvp => kvp.Value.deck == entry.Configuration.Deck && kvp.Value is { unreleased: false, dontOffer: false, rarity: Rarity.common })
				.Where(kvp => entry.Configuration.StarterCards is null || entry.Configuration.StarterCards.All(card => card.Key() != kvp.Key))
				.OrderBy(kvp => kvp.Key)
				// ReSharper disable once MultipleOrderBy
				.OrderBy(_ => random.NextInt())
				.Select(kvp => kvp.Key)
				.Select(key => DB.cards.GetValueOrDefault(key))
				.OfType<Type>()
				.Take(4 - (entry.Configuration.StarterCards?.Count ?? 0))
				.Select(cardType => (Card)Activator.CreateInstance(cardType)!),
			new CannonColorless(),
			new DodgeColorless(),
		];
		
		// source: https://stackoverflow.com/a/36845864
		static int GetStableHashCode(string str)
		{
			var hash1 = 5381;
			var hash2 = hash1;

			for (var i = 0; i < str.Length && str[i] != '\0'; i += 2)
			{
				hash1 = ((hash1 << 5) + hash1) ^ str[i];
				if (i == str.Length - 1 || str[i + 1] == '\0')
					break;
				hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
			}

			return hash1 + hash2 * 1566083941;
		}
	}

	private void SetupAfterDbInit()
	{
		foreach (var entry in this.UniqueNameToPlayableCharacterEntry.Values)
		{
			if (!DB.decks.TryGetValue(entry.Configuration.Deck, out var def))
				continue;
			if (def.starterSoloCards.Count != 0)
				continue;
			def.starterSoloCards = CreateDefaultSoloStarters(entry);
		}
	}

	private static void OnCrystallizedFriendEvent(object? _, List<Choice> choices)
	{
		foreach (var choice in choices)
		{
			var addCharacterIndex = choice.actions.FindIndex(a => a is AAddCharacter);
			if (addCharacterIndex == -1)
				continue;

			var addCharacter = (AAddCharacter)choice.actions[addCharacterIndex];
			if (!DB.decks.TryGetValue(addCharacter.deck, out var def) || !def.canStartRunWith)
				continue;

			choice.actions.InsertRange(
				addCharacterIndex + 1,
				def.starterArtifacts.Select(Mutil.DeepCopy).Select(a => new AAddArtifact
				{
					artifact = a,
					timer = 0
				})
			);
		}
	}

	private void OnModifyBabblePeriod(object? _, ref ShoutPatches.ModifyBabblePeriodEventArgs e)
	{
		if (!this.CharacterTypeToCharacterEntry.TryGetValue(e.Shout.who, out var characterEntry))
			return;
		if (characterEntry.ModOwner == this.VanillaModManifest)
			return;
		if (characterEntry.Babble?.Period is not { } period)
			return;
		e.Period = period;
	}

	private void OnModifyBabbleSound(object? _, ref ShoutPatches.ModifyBabbleSoundEventArgs e)
	{
		if (!this.CharacterTypeToCharacterEntry.TryGetValue(e.Shout.who, out var characterEntry))
			return;
		if (characterEntry.ModOwner == this.VanillaModManifest)
			return;
		if (characterEntry.Babble?.Sound is not { } sound)
			return;
		e.Sound = sound;
	}

	private void OnGetUnlockedChars(object? _, HashSet<Deck> unlockedCharacters)
	{
		foreach (var deck in unlockedCharacters.ToList())
			if (this.Decks.LookupByDeck(deck) is null)
				unlockedCharacters.Remove(deck);

		foreach (var entry in this.UniqueNameToPlayableCharacterEntry.Values)
			if (!entry.Configuration.StartLocked)
				unlockedCharacters.Add(entry.Configuration.Deck);
	}

	private sealed class AnimationEntry(
		IModManifest modOwner,
		string uniqueName,
		CharacterAnimationConfiguration configuration
	) : ICharacterAnimationEntry
	{
		public CharacterAnimationConfiguration Configuration { get; } = configuration;
		
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;

		public override string ToString()
			=> this.UniqueName;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();
	}

	public sealed class PlayableCharacterEntry(
		IModManifest modOwner,
		string uniqueName,
		PlayableCharacterConfiguration configuration,
		IStatusEntry missingStatus,
		Action<PlayableCharacterEntry, PlayableCharacterConfiguration.Amends> amendDelegate
	) : IPlayableCharacterEntry
	{
		public PlayableCharacterConfiguration Configuration { get; set; } = configuration;
		internal bool IsInjected;
		
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public IStatusEntry MissingStatus { get; } = missingStatus;

		public string CharacterType => this.Configuration.Deck == Deck.colorless ? "comp" : this.Configuration.Deck.Key();
		public CharacterBabbleConfiguration? Babble => this.Configuration.Babble;
		public Spr? BorderSprite => this.Configuration.BorderSprite;

		public override string ToString()
			=> this.UniqueName;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();
		
		public void Amend(PlayableCharacterConfiguration.Amends amends)
			=> amendDelegate(this, amends);
	}

	private sealed class NonPlayableCharacterEntry(
		IModManifest modOwner,
		string uniqueName,
		string characterType,
		NonPlayableCharacterConfiguration configuration,
		Action<NonPlayableCharacterEntry, NonPlayableCharacterConfiguration.Amends> amendDelegate
	) : INonPlayableCharacterEntry
	{
		public NonPlayableCharacterConfiguration Configuration { get; set; } = configuration;
		
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public string CharacterType { get; } = characterType;
		
		public CharacterBabbleConfiguration? Babble => this.Configuration.Babble;
		public Spr? BorderSprite => this.Configuration.BorderSprite;

		public override string ToString()
			=> this.UniqueName;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();
		
		public void Amend(NonPlayableCharacterConfiguration.Amends amends)
			=> amendDelegate(this, amends);
	}
}
