using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class ModCharacters(
	IModManifest modManifest,
	Func<CharacterManager> characterManagerProvider
) : IModCharacters
{
	private readonly Dictionary<string, IPlayableCharacterEntry> RegisteredPlayableCharacterStorage = [];
	private readonly Dictionary<string, INonPlayableCharacterEntry> RegisteredNonPlayableCharacterStorage = [];
	private readonly Dictionary<string, ICharacterAnimationEntry> RegisteredCharacterAnimationStorage = [];
	
	public IReadOnlyDictionary<string, IPlayableCharacterEntry> RegisteredPlayableCharacters
		=> this.RegisteredPlayableCharacterStorage;
	
	public IReadOnlyDictionary<string, INonPlayableCharacterEntry> RegisteredNonPlayableCharacters
		=> this.RegisteredNonPlayableCharacterStorage;
	
	public IReadOnlyDictionary<string, ICharacterAnimationEntry> RegisteredCharacterAnimations
		=> this.RegisteredCharacterAnimationStorage;

	public ICharacterAnimationEntry RegisterCharacterAnimation(CharacterAnimationConfiguration configuration)
	{
		var name = $"{configuration.CharacterType}::{configuration.LoopTag}";
		var entry = characterManagerProvider().RegisterCharacterAnimation(modManifest, name, configuration);
		this.RegisteredCharacterAnimationStorage[name] = entry;
		return entry;
	}

	public ICharacterAnimationEntry RegisterCharacterAnimation(string name, CharacterAnimationConfiguration configuration)
	{
		var entry = characterManagerProvider().RegisterCharacterAnimation(modManifest, name, configuration);
		this.RegisteredCharacterAnimationStorage[name] = entry;
		return entry;
	}

	public IPlayableCharacterEntry? LookupByDeck(Deck deck)
		=> characterManagerProvider().LookupByDeck(deck);

	public ICharacterEntry? LookupByCharacterType(string characterType)
		=> characterManagerProvider().LookupByCharacterType(characterType);

	public ICharacterEntry? LookupByUniqueName(string uniqueName)
		=> characterManagerProvider().LookupByUniqueName(uniqueName);

	public IPlayableCharacterEntry RegisterPlayableCharacter(string name, PlayableCharacterConfiguration configuration)
	{
		var entry = characterManagerProvider().RegisterPlayableCharacter(modManifest, name, configuration);
		this.RegisteredPlayableCharacterStorage[name] = entry;
		return entry;
	}

	public INonPlayableCharacterEntry RegisterNonPlayableCharacter(string name, NonPlayableCharacterConfiguration configuration)
	{
		var entry = characterManagerProvider().RegisterNonPlayableCharacter(modManifest, name, configuration);
		this.RegisteredNonPlayableCharacterStorage[name] = entry;
		return entry;
	}
}

internal sealed class VanillaModCharacters(
	Func<CharacterManager> characterManagerProvider
) : IModCharacters
{
	private readonly Lazy<Dictionary<string, IPlayableCharacterEntry>> RegisteredPlayableCharacterStorage = new(
		() => characterManagerProvider().VanillaPlayableCharacterDecks.Value.ToDictionary(
			deck => deck.Key(),
			deck => characterManagerProvider().LookupByDeck(deck)!
		)
	);

	public IReadOnlyDictionary<string, IPlayableCharacterEntry> RegisteredPlayableCharacters
		=> this.RegisteredPlayableCharacterStorage.Value;

	public IReadOnlyDictionary<string, INonPlayableCharacterEntry> RegisteredNonPlayableCharacters
		=> throw new NotImplementedException(); // TODO: maybe implement
	
	public IReadOnlyDictionary<string, ICharacterAnimationEntry> RegisteredCharacterAnimations
		=> throw new NotImplementedException(); // TODO: maybe implement
	
	public ICharacterAnimationEntry RegisterCharacterAnimation(CharacterAnimationConfiguration configuration)
		=> throw new NotSupportedException();

	public ICharacterAnimationEntry RegisterCharacterAnimation(string name, CharacterAnimationConfiguration configuration)
		=> throw new NotSupportedException();

	public IPlayableCharacterEntry? LookupByDeck(Deck deck)
		=> characterManagerProvider().LookupByDeck(deck);

	public ICharacterEntry? LookupByCharacterType(string characterType)
		=> characterManagerProvider().LookupByCharacterType(characterType);

	public ICharacterEntry? LookupByUniqueName(string uniqueName)
		=> characterManagerProvider().LookupByUniqueName(uniqueName);

	public IPlayableCharacterEntry RegisterPlayableCharacter(string name, PlayableCharacterConfiguration configuration)
		=> throw new NotSupportedException();

	public INonPlayableCharacterEntry RegisterNonPlayableCharacter(string name, NonPlayableCharacterConfiguration configuration)
		=> throw new NotSupportedException();
}
