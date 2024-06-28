using System;

namespace Nickel;

internal sealed class ModCharacters : IModCharacters
{
	private IModManifest ModManifest { get; }
	private Func<CharacterManager> CharacterManagerProvider { get; }

	public ModCharacters(IModManifest modManifest, Func<CharacterManager> characterManagerProvider)
	{
		this.ModManifest = modManifest;
		this.CharacterManagerProvider = characterManagerProvider;
		this.V2 = new ModCharactersV2(modManifest, characterManagerProvider);
	}
	
	public IModCharactersV2 V2 { get; }

	public ICharacterAnimationEntry RegisterCharacterAnimation(CharacterAnimationConfiguration configuration)
		=> this.RegisterCharacterAnimation($"{configuration.Deck.Key()}::{configuration.LoopTag}", configuration);

	public ICharacterAnimationEntry RegisterCharacterAnimation(string name, CharacterAnimationConfiguration configuration)
		=> this.CharacterManagerProvider().RegisterCharacterAnimation(this.ModManifest, name, configuration);

	public ICharacterEntry? LookupByDeck(Deck deck)
		=> this.CharacterManagerProvider().LookupByDeck(deck);

	public ICharacterEntry? LookupByUniqueName(string uniqueName)
		=> this.CharacterManagerProvider().LookupByUniqueName(uniqueName);

	public ICharacterEntry RegisterCharacter(string name, CharacterConfiguration configuration)
		=> this.CharacterManagerProvider().RegisterCharacter(this.ModManifest, name, configuration);
}
