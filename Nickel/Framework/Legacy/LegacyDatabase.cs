using CobaltCoreModding.Definitions.ExternalItems;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Nickel;

internal sealed class LegacyDatabase
{
	private Func<ContentManager> ContentManagerProvider { get; }

	private Dictionary<string, ExternalSprite> GlobalNameToSprite { get; } = [];
	private Dictionary<string, ExternalDeck> GlobalNameToDeck { get; } = [];
	private Dictionary<string, ExternalStatus> GlobalNameToStatus { get; } = [];
	private Dictionary<string, ExternalCard> GlobalNameToCard { get; } = [];
	private Dictionary<string, ExternalArtifact> GlobalNameToArtifact { get; } = [];
	private Dictionary<string, ExternalAnimation> GlobalNameToAnimation { get; } = [];
	private Dictionary<string, ExternalCharacter> GlobalNameToCharacter { get; } = [];
	private Dictionary<string, ExternalPartType> GlobalNameToPartType { get; } = [];
	private Dictionary<string, ExternalPart> GlobalNameToPart { get; } = [];
	private Dictionary<string, ExternalShip> GlobalNameToShip { get; } = [];
	private Dictionary<string, Ship> GlobalNameToVanillaShip { get; } = [];
	private Dictionary<string, ExternalStarterShip> GlobalNameToStarterShip { get; } = [];

	private Dictionary<string, ICharacterEntry> GlobalNameToCharacterEntry { get; } = [];
	private Dictionary<string, IPartEntry> GlobalNameToPartEntry { get; } = [];
	private Dictionary<string, IShipEntry> GlobalNameToShipEntry { get; } = [];
	private Dictionary<string, ExternalGlossary> GlobalNameToGlossary { get; } = [];
	private List<ExternalStory> ExternalStories { get; } = [];
	private List<(string, MethodInfo, bool, bool)> ChoiceAndCommands { get; } = [];

	// Used to change the return value of EnumExtensions.Key(this Deck deck)
	private Dictionary<Deck, string>? _reflectedDeckStrings;

	public LegacyDatabase(Func<ContentManager> contentManagerProvider)
	{
		this.ContentManagerProvider = contentManagerProvider;
	}

	public void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		this.InjectCharacterLocalizations(locale, localizations);
		this.InjectGlossaryLocalisations(locale, localizations);
		this.InjectStoryLocalizations(locale, localizations);
	}

	public void AfterDbInit()
	{
		this.InjectGlossaryIconSprites();
		this.InjectExternalStory();
		this.InjectChoiceAndCommands();
	}

	private void InjectCharacterLocalizations(string locale, Dictionary<string, string> localizations)
	{
		// separate localization of characters
		// Nickel localizes Deck names and Character descriptions
		// legacy mods localize both for Characters only
		foreach (var character in this.GlobalNameToCharacter.Values)
		{
			var deck = (Deck)character.Deck.Id!;
			var id = deck.ToString()!;
			var key = deck.Key();

			if (character.GetCharacterName(locale) is { } name)
			{
				localizations[$"char.{key}"] = name;
				localizations[$"char.{key}.name"] = name;
			}
			if (character.GetDesc(locale) is { } description)
			{
				localizations[$"char.{key}.desc"] = description;
				localizations[$"char.{id}.desc"] = description;
			}
		}
	}

	private void InjectGlossaryLocalisations(string locale, Dictionary<string, string> localisations)
	{
		foreach (var glossary in this.GlobalNameToGlossary.Values)
		{
			if (!glossary.GetLocalisation(locale, out var name, out var desc, out var altDesc))
				continue;

			var nameKey = glossary.Head + ".name";
			var descKey = glossary.Head + ".desc";
			var altDescKey = glossary.Head + ".altDesc";

			if (glossary.IntendedOverwrite)
			{
				localisations[nameKey] = name;
				localisations[descKey] = desc;
				if (altDesc is not null)
					localisations[altDescKey] = altDesc;
			}
			else
			{
				localisations.Add(nameKey, name);
				localisations.Add(descKey, desc);
				if (altDesc is not null)
					localisations.Add(altDescKey, altDesc);
			}
		}
	}

	private void InjectStoryLocalizations(string locale, Dictionary<string, string> localisations)
	{
		foreach (var story in this.ExternalStories)
		{
			story.GetLocalisation(locale, out var storyLoc);
			foreach (var (key, value) in storyLoc)
				localisations.Add(key, value);
		}
	}

	internal void InjectGlossaryIconSprites()
	{
		foreach (var glossary in this.GlobalNameToGlossary.Values)
		{
			if (!glossary.Icon.Id.HasValue)
				continue;
			var sprite = (Spr)glossary.Icon.Id.Value;

			if (glossary.IntendedOverwrite)
				DB.icons[glossary.ItemName] = sprite;
			else
				DB.icons.Add(glossary.ItemName, sprite);
		}
	}

	private void InjectExternalStory()
	{
		foreach (var story in this.ExternalStories)
		{
			var node = (StoryNode)story.StoryNode; // validated on registration
			if (story.Instructions is not { } rawInstructions)
			{
				DB.story.all.Add(story.GlobalName, node);
				continue;
			}

			foreach (var rawInstruction in rawInstructions)
			{
				switch (rawInstruction)
				{
					case ExternalStory.ExternalSay extSay:
						node.lines.Add(extSay.ToSay());
						break;
					case ExternalStory.ExternalSaySwitch extSaySwitch:
						node.lines.Add(
							new SaySwitch { lines = extSaySwitch.lines.Select(say => say.ToSay()).ToList() }
						);
						break;
					case Instruction instruction:
						node.lines.Add(instruction);
						break;
					default:
						// validated on registration, shouldn't happen
						throw new InvalidOperationException();
				}
			}

			DB.story.all.Add(story.GlobalName, node);
		}
	}

	private void InjectChoiceAndCommands()
	{
		foreach (var (key, methodInfo, intendedOverride, isChoice) in this.ChoiceAndCommands)
		{
			var dict = isChoice ? DB.eventChoiceFns : DB.storyCommands;
			if (dict.TryAdd(key, methodInfo))
				continue;
			// TODO: log instead of throwing, it's too late to throw now, this breaks other mods
			if (!intendedOverride)
				throw new ArgumentException($"Duplicate choice key", nameof(key));
			dict[key] = methodInfo;
		}
	}

	public void RegisterSprite(IModManifest mod, ExternalSprite value)
	{
		Func<Stream> GetStreamProvider()
		{
			if (value.virtual_location is { } provider)
				return provider;
			if (value.physical_location is { } path)
				return () => path.OpenRead().ToMemoryStream();
			throw new ArgumentException("Unsupported ExternalSprite");
		}

		var entry = this.ContentManagerProvider().Sprites.RegisterSprite(mod, value.GlobalName, GetStreamProvider());
		value.Id = (int)entry.Sprite;
		this.GlobalNameToSprite[value.GlobalName] = value;
	}

	public void RegisterGlossary(IModManifest modManifest, ExternalGlossary glossary) =>
		this.GlobalNameToGlossary[modManifest.UniqueName] = glossary;

	public void RegisterDeck(IModManifest mod, ExternalDeck value)
	{
		DeckConfiguration configuration = new()
		{
			Definition = new() { color = new((uint)value.DeckColor.ToArgb()), titleColor = new((uint)value.TitleColor.ToArgb()) },
			DefaultCardArt = (Spr)value.CardArtDefault.Id!.Value,
			BorderSprite = (Spr)value.BorderSprite.Id!.Value,
			OverBordersSprite = value.BordersOverSprite is null ? null : (Spr)value.BordersOverSprite.Id!.Value
		};
		var entry = this.ContentManagerProvider().Decks.RegisterDeck(mod, value.GlobalName, configuration);
		value.Id = (int)entry.Deck;
		this.GlobalNameToDeck[value.GlobalName] = value;

		this._reflectedDeckStrings ??= AccessTools
			.DeclaredField(typeof(EnumExtensions), "deckStrs")
			.EmitStaticGetter<Dictionary<Deck, string>>()();
		this._reflectedDeckStrings[entry.Deck] = value.GlobalName;
	}

	public void RegisterStatus(IModManifest mod, ExternalStatus value)
	{
		StatusConfiguration configuration = new()
		{
			Definition = new()
			{
				icon = (Spr)value.Icon.Id!.Value,
				color = new((uint)value.MainColor.ToArgb()),
				border = value.BorderColor is { } borderColor ? new((uint)borderColor.ToArgb()) : null,
				affectedByTimestop = value.AffectedByTimestop,
				isGood = value.IsGood
			},
			Name = locale =>
			{
				value.GetLocalisation(locale, out var localized, out _);
				return localized;
			},
			Description = locale =>
			{
				value.GetLocalisation(locale, out _, out var localized);
				return localized;
			}
		};
		var entry = this.ContentManagerProvider().Statuses.RegisterStatus(mod, value.GlobalName, configuration);
		value.Id = (int)entry.Status;
		this.GlobalNameToStatus[value.GlobalName] = value;
	}

	public void RegisterCard(IModManifest mod, ExternalCard value)
	{
		var meta = value.CardType.GetCustomAttribute<CardMeta>() ?? new();
		if (value.ActualDeck is { } deck)
			meta.deck = (Deck)deck.Id!.Value;

		CardConfiguration configuration = new()
		{
			CardType = value.CardType,
			Meta = meta,
			Art = (Spr)value.CardArt.Id!.Value,
			Name = locale =>
			{
				value.GetLocalisation(locale, out var localized, out _, out _, out _);
				return localized;
			},
			Description = locale =>
			{
				value.GetLocalisation(locale, out _, out var localized, out _, out _);
				return localized;
			},
			DescriptionA = locale =>
			{
				value.GetLocalisation(locale, out _, out _, out var localized, out _);
				return localized;
			},
			DescriptionB = locale =>
			{
				value.GetLocalisation(locale, out _, out _, out _, out var localized);
				return localized;
			}
		};

		this.ContentManagerProvider().Cards.RegisterCard(mod, value.GlobalName, configuration);
		this.GlobalNameToCard[value.GlobalName] = value;
	}

	public void RegisterArtifact(IModManifest mod, ExternalArtifact value)
	{
		var meta = value.ArtifactType.GetCustomAttribute<ArtifactMeta>() ?? new();
		if (value.OwnerDeck is { } deck)
			meta.owner = (Deck)deck.Id!.Value;

		ArtifactConfiguration configuration = new()
		{
			ArtifactType = value.ArtifactType,
			Meta = meta,
			Sprite = (Spr)value.Sprite.Id!.Value,
			Name = locale => value.GetLocalisation(locale, out var localized, out _) ? localized : null,
			Description = locale => value.GetLocalisation(locale, out _, out var localized) ? localized : null,
		};

		this.ContentManagerProvider().Artifacts.RegisterArtifact(mod, value.GlobalName, configuration);
		this.GlobalNameToArtifact[value.GlobalName] = value;
	}

	public void RegisterAnimation(IModManifest mod, ExternalAnimation value)
	{
		CharacterAnimationConfiguration configuration = new()
		{
			Deck = (Deck)value.Deck.Id!.Value,
			LoopTag = value.Tag,
			Frames = value.Frames.Select(s => (Spr)s.Id!.Value).ToList()
		};

		this.ContentManagerProvider().Characters.RegisterCharacterAnimation(mod, value.GlobalName, configuration);
		this.GlobalNameToAnimation[value.GlobalName] = value;
	}

	public void RegisterCharacter(IModManifest mod, ExternalCharacter value)
	{
		CharacterConfiguration configuration = new()
		{
			Deck = (Deck)value.Deck.Id!.Value,
			BorderSprite = (Spr)value.CharPanelSpr.Id!.Value,
			StarterArtifactTypes = value.StarterArtifacts.ToList(),
			StarterCardTypes = value.StarterDeck.ToList()
		};

		this.ContentManagerProvider().Characters.RegisterCharacter(mod, value.GlobalName, configuration);
		this.GlobalNameToCharacter[value.GlobalName] = value;
	}

	public void RegisterPartType(IModManifest mod, ExternalPartType value)
	{
		PartTypeConfiguration configuration = new()
		{
			ExclusiveArtifactTypes = value.ExclusiveArtifacts
				.Select(a => a.ArtifactType)
				.Concat(value.ExclusiveNativeArtifacts)
				.ToHashSet()
		};

		this.ContentManagerProvider().Parts.RegisterPartType(mod, value.GlobalName, configuration);
		this.GlobalNameToPartType[value.GlobalName] = value;
	}

	public void RegisterPart(IModManifest mod, ExternalPart value)
	{
		PartConfiguration configuration = new()
		{
			Sprite = (Spr)value.PartSprite.Id!.Value,
			DisabledSprite = value.PartOffSprite is { } partOff ? (Spr)partOff.Id!.Value : null
		};

		var entry = this.ContentManagerProvider().Parts.RegisterPart(mod, value.GlobalName, configuration);
		this.GlobalNameToPart[value.GlobalName] = value;
		this.GlobalNameToPartEntry[value.GlobalName] = entry;
	}

	public void RegisterRawPart(IModManifest mod, string globalName, int spriteId, int? disabledSpriteId)
	{
		PartConfiguration configuration = new()
		{
			Sprite = (Spr)spriteId,
			DisabledSprite = disabledSpriteId is null ? null : (Spr)disabledSpriteId.Value
		};

		var entry = this.ContentManagerProvider().Parts.RegisterPart(mod, globalName, configuration);
		// do not add to `GlobalNameToPart` dictionary - legacy modloader did not, you can't look up these
		this.GlobalNameToPartEntry[globalName] = entry;
	}

	public void RegisterShip(ExternalShip value)
		=> this.GlobalNameToShip[value.GlobalName] = value;

	public void RegisterShip(Ship value, string globalName)
		=> this.GlobalNameToVanillaShip[globalName] = value;

	public void RegisterStarterShip(IModManifest mod, ExternalStarterShip value)
	{
		if (!this.GlobalNameToShip.TryGetValue(value.ShipGlobalName, out var ship))
			throw new ArgumentException("Cannot register a legacy starter ship without a registered ship", nameof(value));

		ShipConfiguration configuration = new()
		{
			Ship = ActualizeExternalStarterShip(value, this.ActualizeShip(value.ShipGlobalName)),
			UnderChassisSprite = ship.ChassisUnderSprite is { } underChassisSprite ? (Spr)underChassisSprite.Id!.Value : null,
			OverChassisSprite = ship.ChassisOverSprite is { } overChassisSprite ? (Spr)overChassisSprite.Id!.Value : null,
			Name = locale =>
			{
				value.GetLocalisations(locale, out var localized, out _);
				return localized;
			},
			Description = locale =>
			{
				value.GetLocalisations(locale, out _, out var localized);
				return localized;
			}
		};

		var entry = this.ContentManagerProvider().Ships.RegisterShip(mod, value.GlobalName, configuration);
		this.GlobalNameToStarterShip[value.GlobalName] = value;
		this.GlobalNameToShipEntry[value.GlobalName] = entry;
	}

	public ExternalSprite GetSprite(string globalName)
		=> this.GlobalNameToSprite.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

	public ExternalGlossary GetGlossary(string globalName)
		=> this.GlobalNameToGlossary.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

	public ExternalDeck GetDeck(string globalName)
		=> this.GlobalNameToDeck.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

	public ExternalStatus GetStatus(string globalName)
		=> this.GlobalNameToStatus.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

	public ExternalCard GetCard(string globalName)
		=> this.GlobalNameToCard.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

	public ExternalArtifact GetArtifact(string globalName)
		=> this.GlobalNameToArtifact.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

	public ExternalAnimation GetAnimation(string globalName)
		=> this.GlobalNameToAnimation.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

	public ExternalCharacter GetCharacter(string globalName)
		=> this.GlobalNameToCharacter.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

	public ExternalPartType GetPartType(string globalName)
		=> this.GlobalNameToPartType.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

	public ExternalPart GetPart(string globalName)
		=> this.GlobalNameToPart.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

	public StarterShip GetStarterShip(string globalName)
		=> this.GlobalNameToShipEntry.TryGetValue(globalName, out var value) ? value.Configuration.Ship : throw new KeyNotFoundException();

	public Ship ActualizeShip(string globalName)
	{
		if (this.GlobalNameToVanillaShip.TryGetValue(globalName, out var ship))
			return Mutil.DeepCopy(ship);
		if (this.GlobalNameToShip.TryGetValue(globalName, out var externalShip))
			return this.ActualizeExternalShip(externalShip);
		throw new KeyNotFoundException();
	}

	private Part ActualizeExternalShipPart(ExternalPart externalPart)
	{
		if (!this.GlobalNameToPartEntry.TryGetValue(externalPart.GlobalName, out var entry))
			throw new KeyNotFoundException();

		var part = externalPart.GetPartObject() is Part partTemplate
			? Mutil.DeepCopy(partTemplate)
			: new();
		part.skin = entry.UniqueName;
		return part;
	}

	private Ship ActualizeExternalShip(ExternalShip externalShip)
	{
		var ship = externalShip.GetShipObject() is Ship shipTemplate
			? Mutil.DeepCopy(shipTemplate)
			: new();

		ship.parts = externalShip.Parts
			.Select(this.ActualizeExternalShipPart)
			.ToList();

		return ship;
	}

	private static StarterShip ActualizeExternalStarterShip(ExternalStarterShip externalStarterShip, Ship ship)
	{
		StarterShip starterShip = new();
		starterShip.ship = ship;
		starterShip.ship.key = externalStarterShip.ShipGlobalName;

		starterShip.artifacts = externalStarterShip.StartingArtifacts
			.Select(a => a.ArtifactType)
			.Concat(externalStarterShip.NativeStartingArtifact)
			.Select(Activator.CreateInstance)
			.OfType<Artifact>()
			.ToList();

		starterShip.cards = externalStarterShip.StartingCards
			.Select(c => c.CardType)
			.Concat(externalStarterShip.NativeStartingCards)
			.Select(Activator.CreateInstance)
			.OfType<Card>()
			.ToList();

		return starterShip;
	}

	public void RegisterStory(ExternalStory story)
	{
		if (story.StoryNode is not StoryNode node)
			throw new ArgumentException($"Provided story node is not of type {typeof(StoryNode)}");
		if (story.Instructions is not { } rawInstructions)
		{
			this.ExternalStories.Add(story);
			return;
		}

		foreach (var rawInstruction in rawInstructions)
		{
			if (rawInstruction is ExternalStory.ExternalSay or ExternalStory.ExternalSaySwitch or Instruction)
				continue;
			throw new ArgumentException(
				$"Cannot add instance of class {rawInstruction.GetType()} to Story Node {story.GlobalName} as it does not inherit from class Instruction",
			);
		}

		this.ExternalStories.Add(story);
	}

	public void RegisterChoiceOrCommand(string key, MethodInfo methodInfo, bool intendedOverride, bool isChoice)
		=> this.ChoiceAndCommands.Add((key, methodInfo, intendedOverride, isChoice));
}
