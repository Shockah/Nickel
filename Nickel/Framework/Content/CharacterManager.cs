using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Nickel;

internal sealed class CharacterManager
{
	private Func<IModManifest, ILogger> LoggerProvider { get; }
	private AfterDbInitManager<AnimationEntry> AnimationManager { get; }
	private AfterDbInitManager<CharacterEntry> CharManager { get; }
	private Dictionary<string, AnimationEntry> UniqueNameToAnimationEntry { get; } = [];
	private Dictionary<string, CharacterEntry> UniqueNameToCharacterEntry { get; } = [];

	public CharacterManager(Func<ModLoadPhase> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider)
	{
		this.LoggerProvider = loggerProvider;
		this.AnimationManager = new(currentModLoadPhaseProvider, Inject);
		this.CharManager = new(currentModLoadPhaseProvider, this.Inject);

		StoryVarsPatches.OnGetUnlockedChars.Subscribe(this.OnGetUnlockedChars);
	}

	internal void InjectQueuedEntries()
	{
		this.AnimationManager.InjectQueuedEntries();
		this.CharManager.InjectQueuedEntries();
	}

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		foreach (var entry in this.UniqueNameToCharacterEntry.Values)
			InjectLocalization(locale, localizations, entry);
	}

	public ICharacterAnimationEntry RegisterCharacterAnimation(IModManifest owner, string name, CharacterAnimationConfiguration configuration)
	{
		AnimationEntry entry = new(owner, $"{owner.UniqueName}::{name}", configuration);
		this.UniqueNameToAnimationEntry[entry.UniqueName] = entry;
		this.AnimationManager.QueueOrInject(entry);
		return entry;
	}

	public ICharacterEntry RegisterCharacter(IModManifest owner, string name, CharacterConfiguration configuration)
	{
		CharacterEntry entry = new(owner, $"{owner.UniqueName}::{name}", configuration);
		this.UniqueNameToCharacterEntry[entry.UniqueName] = entry;
		this.CharManager.QueueOrInject(entry);
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
		NewRunOptions.allChars.Add(entry.Configuration.Deck);
		StarterDeck.starterSets[entry.Configuration.Deck] = new()
		{
			artifacts = entry.Configuration.StarterArtifactTypes?.Select(t => (Artifact)Activator.CreateInstance(t)!)?.ToList() ?? [],
			cards = entry.Configuration.StarterCardTypes.Select(t => (Card)Activator.CreateInstance(t)!).ToList()
		};

		InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}

	private static void InjectLocalization(string locale, Dictionary<string, string> localizations, CharacterEntry entry)
	{
		if (entry.Configuration.Description.Localize(locale) is not { } description)
			return;
		localizations[$"char.{entry.Configuration.Deck.Key()}.desc"] = description;
		localizations[$"char.{entry.Configuration.Deck}.desc"] = description;
	}

	private void OnGetUnlockedChars(object? _, HashSet<Deck> unlockedCharacters)
	{
		foreach (var entry in this.UniqueNameToCharacterEntry.Values)
			if (!entry.Configuration.StartLocked)
				unlockedCharacters.Add(entry.Configuration.Deck);
	}

	private sealed class AnimationEntry(IModManifest modOwner, string uniqueName, CharacterAnimationConfiguration configuration)
		: ICharacterAnimationEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public CharacterAnimationConfiguration Configuration { get; } = configuration;
	}

	private sealed class CharacterEntry(IModManifest modOwner, string uniqueName, CharacterConfiguration configuration)
		: ICharacterEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public CharacterConfiguration Configuration { get; } = configuration;
	}
}
