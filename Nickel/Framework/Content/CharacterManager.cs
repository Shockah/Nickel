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
	private readonly Dictionary<string, ICharacterEntryV2> CharacterTypeToCharacterEntry = [];
	private readonly List<string> VanillaPlayableCharacterDeckNames;
	
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
		StatePatches.OnModifyPotentialExeCards += this.OnModifyPotentialExeCards;
		StoryVarsPatches.OnGetUnlockedChars += this.OnGetUnlockedChars;
		WizardPatches.OnGetAssignableStatuses += this.OnGetAssignableStatuses;

		this.VanillaPlayableCharacterDeckNames = StarterDeck.starterSets.Keys.Select(d => d.Key()).ToList();
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
		var starters = StarterDeck.starterSets[deck];
		var neutralAnimationFrames = DB.charAnimations[alias]["neutral"];
		var miniAnimationFrames = DB.charAnimations[alias]["mini"];
		var startLocked = deck is Deck.goat or Deck.eunice or Deck.hacker or Deck.shard or Deck.colorless;
		var missingStatusColor = DB.statuses[StatusMeta.deckToMissingStatus[deck]].color;
		var missingStatusSprite = DB.statuses[StatusMeta.deckToMissingStatus[deck]].icon;
		var exeCardType = deck switch
		{
			Deck.dizzy => typeof(ColorlessDizzySummon),
			Deck.riggs => typeof(ColorlessRiggsSummon),
			Deck.peri => typeof(ColorlessPeriSummon),
			Deck.goat => typeof(ColorlessIsaacSummon),
			Deck.eunice => typeof(ColorlessDrakeSummon),
			Deck.hacker => typeof(ColorlessMaxSummon),
			Deck.shard => typeof(ColorlessBooksSummon),
			Deck.colorless => typeof(ColorlessCATSummon),
			_ => null
		};
		SingleLocalizationProvider description = _ => Loc.T($"char.{deck}.desc");
		
		var result = new PlayableCharacterEntry(
			modOwner: this.VanillaModManifest,
			uniqueName: key,
			v1: new()
			{
				Deck = deck,
				BorderSprite = borderSprite,
				Starters = starters,
				NeutralAnimation = new()
				{
					Deck = deck,
					LoopTag = "neutral",
					Frames = neutralAnimationFrames
				},
				MiniAnimation = new()
				{
					Deck = deck,
					LoopTag = "mini",
					Frames = miniAnimationFrames
				},
				StartLocked = startLocked,
				MissingStatus = new()
				{
					Color = missingStatusColor,
					Sprite = missingStatusSprite
				},
				ExeCardType = exeCardType,
				Description = description
			},
			v2: new()
			{
				Deck = deck,
				BorderSprite = borderSprite,
				Starters = starters,
				NeutralAnimation = new()
				{
					CharacterType = alias,
					LoopTag = "neutral",
					Frames = neutralAnimationFrames
				},
				MiniAnimation = new()
				{
					CharacterType = alias,
					LoopTag = "mini",
					Frames = miniAnimationFrames
				},
				StartLocked = startLocked,
				MissingStatus = new()
				{
					Color = missingStatusColor,
					Sprite = missingStatusSprite
				},
				ExeCardType = exeCardType,
				Description = description,
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
	
	#region V1
	public ICharacterAnimationEntry RegisterCharacterAnimation(IModManifest owner, string name, CharacterAnimationConfiguration v1)
	{
		var v2 = new CharacterAnimationConfigurationV2
		{
			CharacterType = v1.Deck.Key(),
			LoopTag = v1.LoopTag,
			Frames = v1.Frames
		};
		
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToAnimationEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A character animation with the unique name `{uniqueName}` is already registered", nameof(name));
		var entry = new AnimationEntry(owner, uniqueName, v1, v2);
		this.UniqueNameToAnimationEntry[entry.UniqueName] = entry;
		this.AnimationManager.QueueOrInject(entry);
		return entry;
	}

	public ICharacterEntry? LookupByDeck(Deck deck)
	{
		if (this.DeckToCharacterEntry.TryGetValue(deck, out var entry))
			return entry;
		if (deck is not (Deck.dizzy or Deck.riggs or Deck.peri or Deck.goat or Deck.eunice or Deck.hacker or Deck.shard or Deck.colorless))
			return null;
		return this.CreateForVanilla(deck);
	}

	public ICharacterEntry? LookupByUniqueName(string uniqueName)
	{
		if (this.UniqueNameToPlayableCharacterEntry.TryGetValue(uniqueName, out var entry))
			return entry;

		if (this.VanillaPlayableCharacterDeckNames.Contains(uniqueName))
		{
			var deck = Enum.Parse<Deck>(uniqueName);
			return this.CreateForVanilla(deck);
		}

		return null;
	}

	public PlayableCharacterEntry RegisterCharacter(IModManifest owner, string name, CharacterConfiguration v1)
	{
#pragma warning disable CS0618 // Type or member is obsolete
		if (v1.Starters is not null && (v1.StarterCardTypes is not null || v1.StarterArtifactTypes is not null))
			throw new ArgumentException($"A character should only have `{nameof(CharacterConfiguration.Starters)}` or `{nameof(CharacterConfiguration.StarterCardTypes)}`/`{nameof(CharacterConfiguration.StarterArtifactTypes)}` defined, but not both");
		
		var v2 = new PlayableCharacterConfigurationV2
		{
			Deck = v1.Deck,
			BorderSprite = v1.BorderSprite,
			Starters = v1.Starters ?? new()
			{
				artifacts = v1.StarterArtifactTypes?.Select(t => (Artifact)Activator.CreateInstance(t)!).ToList() ?? [],
				cards = v1.StarterCardTypes?.Select(t => (Card)Activator.CreateInstance(t)!).ToList() ?? []
			},
			NeutralAnimation = v1.NeutralAnimation is { } v1NeutralAnimation
				? new()
				{
					CharacterType = v1.Deck.Key(),
					LoopTag = v1NeutralAnimation.LoopTag,
					Frames = v1NeutralAnimation.Frames
				} : null,
			MiniAnimation = v1.MiniAnimation is { } v1MiniAnimation
				? new()
				{
					CharacterType = v1.Deck.Key(),
					LoopTag = v1MiniAnimation.LoopTag,
					Frames = v1MiniAnimation.Frames
				} : null,
			StartLocked = v1.StartLocked,
			MissingStatus = new()
			{
				Color = v1.MissingStatus.Color,
				Sprite = v1.MissingStatus.Sprite
			},
			ExeCardType = v1.ExeCardType,
			Description = v1.Description
		};
#pragma warning restore CS0618 // Type or member is obsolete

		return this.RegisterPlayableCharacterV2(owner, name, v2, v1);
	}
	#endregion

	#region V2
	public ICharacterAnimationEntryV2 RegisterCharacterAnimationV2(IModManifest owner, string name, CharacterAnimationConfigurationV2 v2)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToAnimationEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A character animation with the unique name `{uniqueName}` is already registered", nameof(name));
		var entry = new AnimationEntry(owner, uniqueName, null, v2);
		this.UniqueNameToAnimationEntry[entry.UniqueName] = entry;
		this.AnimationManager.QueueOrInject(entry);
		return entry;
	}

	public IPlayableCharacterEntryV2? LookupByDeckV2(Deck deck)
	{
		if (this.DeckToCharacterEntry.TryGetValue(deck, out var entry))
			return entry;
		if (deck is not (Deck.dizzy or Deck.riggs or Deck.peri or Deck.goat or Deck.eunice or Deck.hacker or Deck.shard or Deck.colorless))
			return null;
		return this.CreateForVanilla(deck);
	}

	public ICharacterEntryV2? LookupByCharacterTypeV2(string characterType)
	{
		if (this.CharacterTypeToCharacterEntry.TryGetValue(characterType, out var entry))
			return entry;
		
		switch (characterType)
		{
			case "dizzy":
				return this.LookupByDeckV2(Deck.dizzy);
			case "riggs":
				return this.LookupByDeckV2(Deck.riggs);
			case "peri":
				return this.LookupByDeckV2(Deck.peri);
			case "goat":
				return this.LookupByDeckV2(Deck.goat);
			case "eunice":
				return this.LookupByDeckV2(Deck.eunice);
			case "hacker":
				return this.LookupByDeckV2(Deck.hacker);
			case "shard":
				return this.LookupByDeckV2(Deck.shard);
			case "comp":
				return this.LookupByDeckV2(Deck.colorless);
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

	public ICharacterEntryV2? LookupByUniqueNameV2(string uniqueName)
	{
		if (this.UniqueNameToPlayableCharacterEntry.TryGetValue(uniqueName, out var entry))
			return entry;

		if (this.VanillaPlayableCharacterDeckNames.Contains(uniqueName))
		{
			var deck = Enum.Parse<Deck>(uniqueName);
			return this.CreateForVanilla(deck);
		}

		return null;
	}
	
	public PlayableCharacterEntry RegisterPlayableCharacterV2(IModManifest owner, string localName, PlayableCharacterConfigurationV2 v2, CharacterConfiguration? maybeV1 = null)
	{
		if (this.Decks.LookupByDeck(v2.Deck) is not { } deckEntry)
			throw new ArgumentException("Invalid character `Deck`");
		
		var uniqueName = $"{owner.UniqueName}::{localName}";
		if (this.UniqueNameToPlayableCharacterEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A character with the unique name `{uniqueName}` is already registered", nameof(localName));
		
		if (v2.ExeCardType is { } exeCardType && this.Cards.LookupByCardType(exeCardType) is { } exeCardEntry && exeCardEntry.Configuration.Meta.deck != Deck.colorless)
			this.LoggerProvider(owner).LogWarning(
				"Registering a playable character `{Character}` with an EXE card `{Card}` for deck `{Deck}`, but EXE cards should use the `{CatDeck}` deck instead.",
				uniqueName, exeCardEntry.UniqueName, exeCardEntry.Configuration.Meta.deck.Key(), Deck.colorless.Key()
			);
		
		var v1 = maybeV1 ?? new CharacterConfiguration
		{
			Deck = v2.Deck,
			BorderSprite = v2.BorderSprite,
			Starters = v2.Starters,
			NeutralAnimation = v2.NeutralAnimation is { } v2NeutralAnimation
				? new()
				{
					Deck = v2.Deck,
					LoopTag = v2NeutralAnimation.LoopTag,
					Frames = v2NeutralAnimation.Frames
				} : null,
			MiniAnimation = v2.MiniAnimation is { } v2MiniAnimation
				? new()
				{
					Deck = v2.Deck,
					LoopTag = v2MiniAnimation.LoopTag,
					Frames = v2MiniAnimation.Frames
				} : null,
			StartLocked = v2.StartLocked,
			MissingStatus = new()
			{
				Color = v2.MissingStatus.Color,
				Sprite = v2.MissingStatus.Sprite
			},
			ExeCardType = v2.ExeCardType,
			Description = v2.Description
		};
		
		var missingStatus = this.RegisterMissingStatus(owner, $"{localName}::MissingStatus", deckEntry, v2.MissingStatus.Color, v2.MissingStatus.Sprite);
		var entry = new PlayableCharacterEntry(owner, uniqueName, v1, v2, missingStatus, this.Amend);
		this.UniqueNameToPlayableCharacterEntry[entry.UniqueName] = entry;
		this.DeckToCharacterEntry[entry.V2.Deck] = entry;
		this.CharacterTypeToCharacterEntry[entry.CharacterType] = entry;
		this.PlayableCharacterManager.QueueOrInject(entry);
		return entry;
	}
	
	public INonPlayableCharacterEntryV2 RegisterNonPlayableCharacterV2(IModManifest owner, string name, NonPlayableCharacterConfigurationV2 v2)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToPlayableCharacterEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A character with the unique name `{uniqueName}` is already registered", nameof(name));
		
		var characterType = $"{owner.UniqueName}::{v2.CharacterType}";
		var entry = new NonPlayableCharacterEntry(owner, uniqueName, characterType, v2, this.Amend);
		this.UniqueNameToNonPlayableCharacterEntry[entry.UniqueName] = entry;
		this.CharacterTypeToCharacterEntry[entry.CharacterType] = entry;
		this.NonPlayableCharacterManager.QueueOrInject(entry);
		return entry;
	}
	#endregion

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
		=> this.Decks.QueueDeckOrderUpdate();

	private static void Inject(AnimationEntry entry)
	{
		ref var characterAnimations = ref CollectionsMarshal.GetValueRefOrAddDefault(DB.charAnimations, entry.V2.CharacterType, out var characterAnimationsExists);
		if (!characterAnimationsExists)
			characterAnimations = [];
		characterAnimations![entry.V2.LoopTag] = entry.V2.Frames.ToList();
	}

	private void Inject(PlayableCharacterEntry entry)
	{
		if (entry.V2.NeutralAnimation is { } neutralAnimationConfiguration)
		{
			if (neutralAnimationConfiguration.LoopTag != "neutral")
			{
				this.LoggerProvider(entry.ModOwner).LogError($"Could not inject character {{Character}}: `{nameof(CharacterConfiguration.NeutralAnimation)}` is not tagged `neutral`.", entry.UniqueName);
				return;
			}
			this.RegisterCharacterAnimationV2(entry.ModOwner, $"{entry.UniqueName}::neutral", neutralAnimationConfiguration);
		}
		if (entry.V2.MiniAnimation is { } miniAnimationConfiguration)
		{
			if (miniAnimationConfiguration.LoopTag != "mini")
			{
				this.LoggerProvider(entry.ModOwner).LogError($"Could not inject character {{Character}}: `{nameof(CharacterConfiguration.MiniAnimation)}` is not tagged `mini`.", entry.UniqueName);
				return;
			}
			this.RegisterCharacterAnimationV2(entry.ModOwner, $"{entry.UniqueName}::mini", miniAnimationConfiguration);
		}

		DB.charPanels[entry.CharacterType] = entry.V2.BorderSprite;
		
		NewRunOptions.allChars = NewRunOptions.allChars
			.Append(entry.V2.Deck)
			.Select(this.Decks.LookupByDeck)
			.Where(e => e is not null)
			.Select(e => e!)
			.OrderBy(e => e.ModOwner == this.VanillaModManifest ? "" : e.ModOwner.UniqueName)
			.Select(e => e.Deck)
			.ToList();
		
		StarterDeck.starterSets[entry.V2.Deck] = entry.V2.Starters;
		StatusMeta.deckToMissingStatus[entry.V2.Deck] = entry.MissingStatus.Status;

		if (entry.V2.SoloStarters is { } soloStarters)
			SoloStarterDeck.soloStarterSets[entry.V2.Deck] = soloStarters;

		this.InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);

		entry.IsInjected = true;
		
		this.QueueDeckOrderUpdate();
	}
	
	private void Amend(PlayableCharacterEntry entry, PlayableCharacterConfigurationV2.Amends amends)
	{
		if (!this.UniqueNameToPlayableCharacterEntry.ContainsKey(entry.UniqueName))
			throw new ArgumentException($"A character with the unique name `{entry.UniqueName}` is not registered");

		if (!entry.IsInjected)
		{
			Finish();
			return;
		}

		if (amends.SoloStarters is { } soloStarters)
		{
			if (soloStarters.Value is null)
			{
				if (SoloStarterDeck.soloStarterSets.ContainsKey(entry.V2.Deck))
					SoloStarterDeck.soloStarterSets[entry.V2.Deck] = CreateDefaultSoloStarters(entry);
				else
					SoloStarterDeck.soloStarterSets.Remove(entry.V2.Deck);
			}
			else
			{
				SoloStarterDeck.soloStarterSets[entry.V2.Deck] = soloStarters.Value;
			}
		}

		Finish();

		void Finish()
		{
			if (amends.SoloStarters is { } soloStarters)
				entry.V2 = entry.V2 with { SoloStarters = soloStarters.Value };
			if (amends.Babble is { } babble)
				entry.V2 = entry.V2 with { Babble = babble.Value };
			
			if (amends.ExeCardType is { } exeCardType)
			{
				entry.V2 = entry.V2 with { ExeCardType = exeCardType.Value };
				entry.V1 = entry.V1 with { ExeCardType = exeCardType.Value };
			}
		}
	}
	
	private void Inject(NonPlayableCharacterEntry entry)
	{
		if (entry.V2.NeutralAnimation is { } neutralAnimationConfiguration)
		{
			if (neutralAnimationConfiguration.LoopTag != "neutral")
			{
				this.LoggerProvider(entry.ModOwner).LogError($"Could not inject character {{Character}}: `{nameof(CharacterConfiguration.NeutralAnimation)}` is not tagged `neutral`.", entry.UniqueName);
				return;
			}
			this.RegisterCharacterAnimationV2(entry.ModOwner, $"{entry.UniqueName}::neutral", neutralAnimationConfiguration);
		}

		if (entry.V2.BorderSprite is { } borderSprite)
			DB.charPanels[entry.CharacterType] = borderSprite;

		this.InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}
	
	private void Amend(NonPlayableCharacterEntry entry, NonPlayableCharacterConfigurationV2.Amends amends)
	{
		if (!this.UniqueNameToNonPlayableCharacterEntry.ContainsKey(entry.UniqueName))
			throw new ArgumentException($"A character with the unique name `{entry.UniqueName}` is not registered");

		if (amends.Babble is { } babble)
			entry.V2 = entry.V2 with { Babble = babble.Value };
	}

	private void InjectLocalization(string locale, Dictionary<string, string> localizations, PlayableCharacterEntry entry)
	{
		if (entry.ModOwner == this.VanillaModManifest)
			return;
		if (entry.V2.Description.Localize(locale) is { } description)
		{
			localizations[$"char.{entry.V2.Deck.Key()}.desc"] = description;
			localizations[$"char.{entry.V2.Deck}.desc"] = description;
		}
		if (this.Decks.LookupByDeck(entry.V2.Deck) is { } deckEntry)
		{
			var characterName = deckEntry.Configuration.Name?.Invoke(locale);
			localizations[$"char.{entry.V2.Deck}.desc.missing"] = $"<c={deckEntry.Configuration.Definition.color}>{characterName?.ToUpper()}..?</c>\n{characterName} is missing.";
		}
	}

	private void InjectLocalization(string locale, Dictionary<string, string> localizations, NonPlayableCharacterEntry entry)
	{
		if (entry.ModOwner == this.VanillaModManifest)
			return;
		if (entry.V2.Name.Localize(locale) is not { } name)
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

	private static StarterDeck CreateDefaultSoloStarters(PlayableCharacterEntry entry)
	{
		var random = new Rand((uint)GetStableHashCode(entry.UniqueName));

		return new()
		{
			artifacts = entry.V2.Starters.artifacts.ToList(),
			cards =
			[
				.. entry.V2.Starters.cards,
				.. DB.cardMetas
					.Where(kvp => kvp.Value.deck == entry.V2.Deck && kvp.Value is { unreleased: false, dontOffer: false, rarity: Rarity.common })
					.Where(kvp => entry.V2.Starters.cards.All(card => card.Key() != kvp.Key))
					.OrderBy(kvp => kvp.Key)
					// ReSharper disable once MultipleOrderBy
					.OrderBy(_ => random.NextInt())
					.Select(kvp => kvp.Key)
					.Select(key => DB.cards.GetValueOrDefault(key))
					.OfType<Type>()
					.Take(4 - entry.V2.Starters.cards.Count)
					.Select(cardType => (Card)Activator.CreateInstance(cardType)!),
				new CannonColorless(),
				new DodgeColorless(),
			]
		};
		
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
			if (!SoloStarterDeck.soloStarterSets.ContainsKey(entry.V2.Deck))
				SoloStarterDeck.soloStarterSets[entry.V2.Deck] = CreateDefaultSoloStarters(entry);
	}

	private static void OnCrystallizedFriendEvent(object? _, List<Choice> choices)
	{
		foreach (var choice in choices)
		{
			var addCharacterIndex = choice.actions.FindIndex(a => a is AAddCharacter);
			if (addCharacterIndex == -1)
				continue;

			var addCharacter = (AAddCharacter)choice.actions[addCharacterIndex];
			if (!StarterDeck.starterSets.TryGetValue(addCharacter.deck, out var starter))
				continue;

			choice.actions.InsertRange(
				addCharacterIndex + 1,
				starter.artifacts.Select(Mutil.DeepCopy).Select(a => new AAddArtifact
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

	private void OnModifyPotentialExeCards(object? _, ref StatePatches.ModifyPotentialExeCardsEventArgs e)
	{
		foreach (var character in this.UniqueNameToPlayableCharacterEntry.Values)
		{
			var configurationV2 = ((IPlayableCharacterEntryV2)character).Configuration;
			
			if (configurationV2.ExeCardType is not { } exeCardType)
				continue;
			if (e.Characters.Contains(configurationV2.Deck))
				continue;
			if (e.ExeCards.Any(c => c.GetType() == exeCardType))
				continue;
			e.ExeCards.Add((Card)Activator.CreateInstance(exeCardType)!);
		}
	}

	private void OnGetUnlockedChars(object? _, HashSet<Deck> unlockedCharacters)
	{
		foreach (var deck in unlockedCharacters.ToList())
			if (this.Decks.LookupByDeck(deck) is null)
				unlockedCharacters.Remove(deck);

		foreach (var entry in this.UniqueNameToPlayableCharacterEntry.Values)
		{
			var configurationV2 = ((IPlayableCharacterEntryV2)entry).Configuration;
			if (!configurationV2.StartLocked)
				unlockedCharacters.Add(configurationV2.Deck);
		}
	}

	private void OnGetAssignableStatuses(object? _, ref WizardPatches.GetAssignableStatusesEventArgs e)
	{
		e.Statuses.RemoveAll(s => s == Status.heat);
		foreach (var character in e.State.characters)
		{
			if (character.deckType is not { } deck)
				continue;
			if (this.UniqueNameToPlayableCharacterEntry.Values.FirstOrDefault(e => e.V2.Deck == deck) is not { } entry)
				continue;
			e.Statuses.Add(entry.MissingStatus.Status);
		}
	}

	private sealed class AnimationEntry(
		IModManifest modOwner,
		string uniqueName,
		CharacterAnimationConfiguration? v1,
		CharacterAnimationConfigurationV2 v2
	) : ICharacterAnimationEntry, ICharacterAnimationEntryV2
	{
		internal CharacterAnimationConfiguration? V1 { get; } = v1;
		internal CharacterAnimationConfigurationV2 V2 { get; } = v2;
		
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;

		CharacterAnimationConfiguration ICharacterAnimationEntry.Configuration => this.V1 ?? throw new InvalidOperationException();
		
		CharacterAnimationConfigurationV2 ICharacterAnimationEntryV2.Configuration => this.V2;

		public override string ToString()
			=> this.UniqueName;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();
	}

	public sealed class PlayableCharacterEntry(
		IModManifest modOwner,
		string uniqueName,
		CharacterConfiguration v1,
		PlayableCharacterConfigurationV2 v2,
		IStatusEntry missingStatus,
		Action<PlayableCharacterEntry, PlayableCharacterConfigurationV2.Amends> amendDelegate
	) : ICharacterEntry, IPlayableCharacterEntryV2
	{
		internal CharacterConfiguration V1 { get; set; } = v1;
		internal PlayableCharacterConfigurationV2 V2 { get; set; } = v2;
		internal bool IsInjected;
		
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public IStatusEntry MissingStatus { get; } = missingStatus;

		public string CharacterType => this.V2.Deck == Deck.colorless ? "comp" : this.V2.Deck.Key();
		public CharacterBabbleConfiguration? Babble => this.V2.Babble;

		CharacterConfiguration ICharacterEntry.Configuration => this.V1;
		
		PlayableCharacterConfigurationV2 IPlayableCharacterEntryV2.Configuration => this.V2;
		Spr? ICharacterEntryV2.BorderSprite => this.V2.BorderSprite;

		public override string ToString()
			=> this.UniqueName;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();
		
		public void Amend(PlayableCharacterConfigurationV2.Amends amends)
			=> amendDelegate(this, amends);
	}

	private sealed class NonPlayableCharacterEntry(
		IModManifest modOwner,
		string uniqueName,
		string characterType,
		NonPlayableCharacterConfigurationV2 v2,
		Action<NonPlayableCharacterEntry, NonPlayableCharacterConfigurationV2.Amends> amendDelegate
	) : INonPlayableCharacterEntryV2
	{
		internal NonPlayableCharacterConfigurationV2 V2 { get; set; } = v2;
		
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public string CharacterType { get; } = characterType;
		public CharacterBabbleConfiguration? Babble => this.V2.Babble;
		
		NonPlayableCharacterConfigurationV2 INonPlayableCharacterEntryV2.Configuration => this.V2;
		Spr? ICharacterEntryV2.BorderSprite => this.V2.BorderSprite;

		public override string ToString()
			=> this.UniqueName;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();
		
		public void Amend(NonPlayableCharacterConfigurationV2.Amends amends)
			=> amendDelegate(this, amends);
	}
}
