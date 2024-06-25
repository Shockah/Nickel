using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Nickel;

internal sealed class CharacterManager
{
	private Func<IModManifest, ILogger> LoggerProvider { get; }
	private SpriteManager Sprites { get; }
	private DeckManager Decks { get; }
	private StatusManager Statuses { get; }
	private IModManifest VanillaModManifest { get; }
	private AfterDbInitManager<AnimationEntry> AnimationManager { get; }
	private AfterDbInitManager<CharacterEntry> CharManager { get; }
	private Dictionary<string, AnimationEntry> UniqueNameToAnimationEntry { get; } = [];
	private Dictionary<string, CharacterEntry> UniqueNameToCharacterEntry { get; } = [];
	private Dictionary<Deck, CharacterEntry> DeckToCharacterEntry { get; } = [];

	public CharacterManager(
		Func<ModLoadPhase> currentModLoadPhaseProvider,
		Func<IModManifest, ILogger> loggerProvider,
		SpriteManager sprites,
		DeckManager decks,
		StatusManager statuses,
		IModManifest vanillaModManifest
	)
	{
		this.LoggerProvider = loggerProvider;
		this.Sprites = sprites;
		this.Decks = decks;
		this.Statuses = statuses;
		this.VanillaModManifest = vanillaModManifest;
		this.AnimationManager = new(currentModLoadPhaseProvider, Inject);
		this.CharManager = new(currentModLoadPhaseProvider, this.Inject);

		EventsPatches.OnCrystallizedFriendEvent.Subscribe(this.OnCrystallizedFriendEvent);
		StatePatches.OnModifyPotentialExeCards.Subscribe(this.OnModifyPotentialExeCards);
		StoryVarsPatches.OnGetUnlockedChars.Subscribe(this.OnGetUnlockedChars);
		WizardPatches.OnGetAssignableStatuses.Subscribe(this.OnGetAssignableStatuses);
	}

	internal void InjectQueuedEntries()
	{
		this.AnimationManager.InjectQueuedEntries();
		this.CharManager.InjectQueuedEntries();
	}

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		foreach (var entry in this.UniqueNameToCharacterEntry.Values)
			this.InjectLocalization(locale, localizations, entry);
	}

	public ICharacterAnimationEntry RegisterCharacterAnimation(IModManifest owner, string name, CharacterAnimationConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToAnimationEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A character animation with the unique name `{uniqueName}` is already registered", nameof(name));
		AnimationEntry entry = new(owner, uniqueName, configuration);
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

		return new CharacterEntry(
			modOwner: this.VanillaModManifest,
			uniqueName: Enum.GetName(deck)!,
			configuration: new()
			{
				Deck = deck,
				BorderSprite = DB.charPanels[deck.Key()],
				Starters = StarterDeck.starterSets[deck],
				NeutralAnimation = new()
				{
					Deck = deck,
					LoopTag = "neutral",
					Frames = DB.charAnimations[Character.GetSpriteAliasIfExists(deck.Key())]["neutral"]
				},
				MiniAnimation = new()
				{
					Deck = deck,
					LoopTag = "mini",
					Frames = DB.charAnimations[Character.GetSpriteAliasIfExists(deck.Key())]["mini"]
				},
				StartLocked = deck is Deck.goat or Deck.eunice or Deck.hacker or Deck.shard,
				MissingStatus = new()
				{
					Color = DB.statuses[StatusMeta.deckToMissingStatus[deck]].color,
					Sprite = DB.statuses[StatusMeta.deckToMissingStatus[deck]].icon
				},
				ExeCardType = deck switch
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
				},
				Description = _ => Loc.T($"char.{deck}.desc")
			},
			missingStatus: this.Statuses.LookupByStatus(StatusMeta.deckToMissingStatus[deck])!
		);
	}

	public ICharacterEntry? LookupByUniqueName(string uniqueName)
		=> this.UniqueNameToCharacterEntry.GetValueOrDefault(uniqueName);

	public ICharacterEntry RegisterCharacter(IModManifest owner, string name, CharacterConfiguration configuration)
	{
#pragma warning disable CS0618 // Type or member is obsolete
		if (configuration.Starters is not null && (configuration.StarterCardTypes is not null || configuration.StarterArtifactTypes is not null))
			throw new ArgumentException($"A character should only have `{nameof(CharacterConfiguration.Starters)}` or `{nameof(CharacterConfiguration.StarterCardTypes)}`/`{nameof(CharacterConfiguration.StarterArtifactTypes)}` defined, but not both");
#pragma warning restore CS0618 // Type or member is obsolete

		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToCharacterEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A character with the unique name `{uniqueName}` is already registered", nameof(name));
		var missingStatus = this.RegisterMissingStatus(owner, $"{name}::MissingStatus", configuration, configuration.MissingStatus);
		CharacterEntry entry = new(owner, uniqueName, configuration, missingStatus);
		this.UniqueNameToCharacterEntry[entry.UniqueName] = entry;
		this.DeckToCharacterEntry[entry.Configuration.Deck] = entry;
		this.CharManager.QueueOrInject(entry);
		return entry;
	}

	private IStatusEntry RegisterMissingStatus(
		IModManifest owner,
		string name,
		CharacterConfiguration characterConfiguration,
		CharacterConfiguration.MissingStatusConfiguration configuration
	)
	{
		var deck = this.Decks.LookupByDeck(characterConfiguration.Deck) ?? throw new ArgumentException("Invalid character `Deck`");
		var sprite = configuration.Sprite;
		var color = configuration.Color ?? deck.Configuration.Definition.color;

		sprite ??= this.Sprites.RegisterSprite(owner, $"{name}::Icon", () =>
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
			}).Sprite;

		return this.Statuses.RegisterStatus(owner, name, new()
		{
			Definition = new()
			{
				color = color,
				icon = sprite.Value,
				isGood = false
			},
			Name = locale => $"{deck.Configuration.Name?.Invoke(locale)} is missing", // TODO: localize
			Description = locale => $"The next {{0}} <c={color}>{deck.Configuration.Name?.Invoke(locale)}</c> cards you play do nothing.", // TODO: localize
		});
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
			entry = default;
			return false;
		}
	}

	public bool TryGetCharacterByUniqueName(string uniqueName, [MaybeNullWhen(false)] out ICharacterEntry entry)
	{
		if (this.UniqueNameToCharacterEntry.TryGetValue(uniqueName, out var typedEntry))
		{
			entry = typedEntry;
			return true;
		}
		else
		{
			entry = default;
			return false;
		}
	}

	private static void Inject(AnimationEntry entry)
	{
		if (!DB.charAnimations.TryGetValue(entry.Configuration.Deck.Key(), out var characterAnimations))
		{
			characterAnimations = [];
			DB.charAnimations[entry.Configuration.Deck.Key()] = characterAnimations;
		}
		characterAnimations[entry.Configuration.LoopTag] = entry.Configuration.Frames.ToList();
	}

	private void Inject(CharacterEntry entry)
	{
		if (entry.Configuration.NeutralAnimation is { } neutralAnimationConfiguration)
		{
			if (neutralAnimationConfiguration.LoopTag != "neutral")
			{
				this.LoggerProvider(entry.ModOwner).LogError($"Could not inject character {{Character}}: `{nameof(CharacterConfiguration.NeutralAnimation)}` is not tagged `neutral`.", entry.UniqueName);
				return;
			}
			this.RegisterCharacterAnimation(entry.ModOwner, $"{entry.UniqueName}::neutral", neutralAnimationConfiguration);
		}
		if (entry.Configuration.MiniAnimation is { } miniAnimationConfiguration)
		{
			if (miniAnimationConfiguration.LoopTag != "mini")
			{
				this.LoggerProvider(entry.ModOwner).LogError($"Could not inject character {{Character}}: `{nameof(CharacterConfiguration.MiniAnimation)}` is not tagged `mini`.", entry.UniqueName);
				return;
			}
			this.RegisterCharacterAnimation(entry.ModOwner, $"{entry.UniqueName}::mini", miniAnimationConfiguration);
		}

		if (entry.Configuration.NeutralAnimation is null || entry.Configuration.MiniAnimation is null)
		{
			if (!DB.charAnimations.TryGetValue(entry.Configuration.Deck.Key(), out var charAnimations))
			{
				this.LoggerProvider(entry.ModOwner).LogError($"Could not inject character {{Character}}: the `neutral` and `mini` animations are not registered.", entry.UniqueName);
				return;
			}
			if (!charAnimations.ContainsKey("neutral"))
			{
				this.LoggerProvider(entry.ModOwner).LogError($"Could not inject character {{Character}}: the `neutral` animation is not registered.", entry.UniqueName);
				return;
			}
			if (!charAnimations.ContainsKey("mini"))
			{
				this.LoggerProvider(entry.ModOwner).LogError($"Could not inject character {{Character}}: the `mini` animation is not registered.", entry.UniqueName);
				return;
			}
		}

		DB.charPanels[entry.Configuration.Deck.Key()] = entry.Configuration.BorderSprite;
		NewRunOptions.allChars = NewRunOptions.allChars
			.Append(entry.Configuration.Deck)
			.Select(this.Decks.LookupByDeck)
			.Where(e => e != null)
			.Select(e => e!)
			.OrderBy(e => e.ModOwner == this.VanillaModManifest ? "" : e.ModOwner.UniqueName)
			.Select(e => e.Deck)
			.ToList();
#pragma warning disable CS0618 // Type or member is obsolete
		StarterDeck.starterSets[entry.Configuration.Deck] = entry.Configuration.Starters ?? new()
		{
			artifacts = entry.Configuration.StarterArtifactTypes?.Select(t => (Artifact)Activator.CreateInstance(t)!).ToList() ?? [],
			cards = entry.Configuration.StarterCardTypes?.Select(t => (Card)Activator.CreateInstance(t)!).ToList() ?? []
		};
#pragma warning restore CS0618 // Type or member is obsolete

		StatusMeta.deckToMissingStatus[entry.Configuration.Deck] = entry.MissingStatus.Status;

		this.InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}

	private void InjectLocalization(string locale, Dictionary<string, string> localizations, CharacterEntry entry)
	{
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

	private void OnCrystallizedFriendEvent(object? _, List<Choice> choices)
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

	private void OnModifyPotentialExeCards(object? _, StatePatches.ModifyPotentialExeCardsEventArgs e)
	{
		foreach (var character in this.UniqueNameToCharacterEntry.Values)
		{
			if (character.Configuration.ExeCardType is not { } exeCardType)
				continue;
			if (e.Characters.Contains(character.Configuration.Deck))
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

		foreach (var entry in this.UniqueNameToCharacterEntry.Values)
			if (!entry.Configuration.StartLocked)
				unlockedCharacters.Add(entry.Configuration.Deck);
	}

	private void OnGetAssignableStatuses(object? _, WizardPatches.GetAssignableStatusesEventArgs e)
	{
		e.Statuses.RemoveAll(s => s == Status.heat);
		foreach (var character in e.State.characters)
		{
			if (character.deckType is not { } deck)
				continue;
			if (this.UniqueNameToCharacterEntry.Values.FirstOrDefault(e => e.Configuration.Deck == deck) is not { } entry)
				continue;
			e.Statuses.Add(entry.MissingStatus.Status);
		}
	}

	private sealed class AnimationEntry(IModManifest modOwner, string uniqueName, CharacterAnimationConfiguration configuration)
		: ICharacterAnimationEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public CharacterAnimationConfiguration Configuration { get; } = configuration;
	}

	private sealed class CharacterEntry(IModManifest modOwner, string uniqueName, CharacterConfiguration configuration, IStatusEntry missingStatus)
		: ICharacterEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public CharacterConfiguration Configuration { get; } = configuration;
		public IStatusEntry MissingStatus { get; } = missingStatus;
	}
}
