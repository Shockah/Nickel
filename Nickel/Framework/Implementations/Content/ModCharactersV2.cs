using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class ModCharactersV2(
	IModManifest modManifest,
	Func<CharacterManager> characterManagerProvider
) : IModCharactersV2
{
	public IReadOnlyDictionary<string, IPlayableCharacterEntryV2> RegisteredPlayableCharacters
		=> this.RegisteredPlayableCharacterStorage;
	
	public IReadOnlyDictionary<string, INonPlayableCharacterEntryV2> RegisteredNonPlayableCharacters
		=> this.RegisteredNonPlayableCharacterStorage;
	
	public IReadOnlyDictionary<string, ICharacterAnimationEntryV2> RegisteredCharacterAnimations
		=> this.RegisteredCharacterAnimationStorage;

	private readonly Dictionary<string, IPlayableCharacterEntryV2> RegisteredPlayableCharacterStorage = [];
	private readonly Dictionary<string, INonPlayableCharacterEntryV2> RegisteredNonPlayableCharacterStorage = [];
	private readonly Dictionary<string, ICharacterAnimationEntryV2> RegisteredCharacterAnimationStorage = [];

	public ICharacterAnimationEntryV2 RegisterCharacterAnimation(CharacterAnimationConfigurationV2 configuration)
	{
		var name = $"{configuration.CharacterType}::{configuration.LoopTag}";
		var entry = characterManagerProvider().RegisterCharacterAnimationV2(modManifest, name, configuration);
		this.RegisteredCharacterAnimationStorage[name] = entry;
		return entry;
	}

	public ICharacterAnimationEntryV2 RegisterCharacterAnimation(string name, CharacterAnimationConfigurationV2 configuration)
	{
		var entry = characterManagerProvider().RegisterCharacterAnimationV2(modManifest, name, configuration);
		this.RegisteredCharacterAnimationStorage[name] = entry;
		return entry;
	}

	public IPlayableCharacterEntryV2? LookupByDeck(Deck deck)
		=> characterManagerProvider().LookupByDeckV2(deck);

	public ICharacterEntryV2? LookupByCharacterType(string characterType)
		=> characterManagerProvider().LookupByCharacterTypeV2(characterType);

	public ICharacterEntryV2? LookupByUniqueName(string uniqueName)
		=> characterManagerProvider().LookupByUniqueNameV2(uniqueName);

	public IPlayableCharacterEntryV2 RegisterPlayableCharacter(string name, PlayableCharacterConfigurationV2 configuration)
	{
		var entry = characterManagerProvider().RegisterPlayableCharacterV2(modManifest, name, configuration);
		this.RegisteredPlayableCharacterStorage[name] = entry;
		return entry;
	}

	public INonPlayableCharacterEntryV2 RegisterNonPlayableCharacter(string name, NonPlayableCharacterConfigurationV2 configuration)
	{
		var entry = characterManagerProvider().RegisterNonPlayableCharacterV2(modManifest, name, configuration);
		this.RegisteredNonPlayableCharacterStorage[name] = entry;
		return entry;
	}
}

internal sealed class VanillaModCharactersV2(
	IModManifest modManifest,
	Func<CharacterManager> characterManagerProvider
) : IModCharactersV2
{
	private readonly Lazy<Dictionary<string, IPlayableCharacterEntryV2>> LazyRegisteredPlayableCharacters = new(
		() => NewRunOptions.allChars
			.Select(d => characterManagerProvider().LookupByDeckV2(d))
			.Where(e => e?.ModOwner == modManifest)
			.ToDictionary(e => e!.UniqueName, e => e!)
	);
	
	public IReadOnlyDictionary<string, IPlayableCharacterEntryV2> RegisteredPlayableCharacters
		=> this.LazyRegisteredPlayableCharacters.Value;
	
	// TODO: maybe implement one day
	public IReadOnlyDictionary<string, INonPlayableCharacterEntryV2> RegisteredNonPlayableCharacters
		=> throw new NotImplementedException();
	
	// TODO: maybe implement one day
	public IReadOnlyDictionary<string, ICharacterAnimationEntryV2> RegisteredCharacterAnimations
		=> throw new NotImplementedException();
	
	public ICharacterAnimationEntryV2 RegisterCharacterAnimation(CharacterAnimationConfigurationV2 configuration)
		=> throw new NotSupportedException();

	public ICharacterAnimationEntryV2 RegisterCharacterAnimation(string name, CharacterAnimationConfigurationV2 configuration)
		=> throw new NotSupportedException();

	public IPlayableCharacterEntryV2? LookupByDeck(Deck deck)
		=> characterManagerProvider().LookupByDeckV2(deck);

	public ICharacterEntryV2? LookupByCharacterType(string characterType)
		=> characterManagerProvider().LookupByCharacterTypeV2(characterType);

	public ICharacterEntryV2? LookupByUniqueName(string uniqueName)
		=> characterManagerProvider().LookupByUniqueNameV2(uniqueName);

	public IPlayableCharacterEntryV2 RegisterPlayableCharacter(string name, PlayableCharacterConfigurationV2 configuration)
		=> throw new NotSupportedException();

	public INonPlayableCharacterEntryV2 RegisterNonPlayableCharacter(string name, NonPlayableCharacterConfigurationV2 configuration)
		=> throw new NotSupportedException();
}
