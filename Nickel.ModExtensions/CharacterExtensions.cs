namespace Nickel.ModExtensions;

public static class CharacterExtensions
{
	extension(Deck deck)
	{
		/// <summary>
		/// The playable character entry for this <see cref="Deck"/>, if it's registered.
		/// </summary>
		public IPlayableCharacterEntry? Character
			=> ModExtensions.Helper.Content.Characters.LookupByDeck(deck);
	}
	
	extension(Character character)
	{
		/// <summary>
		/// The character entry for this <see cref="Character"/>, if it's registered.
		/// </summary>
		public ICharacterEntry? Character
			=> ModExtensions.Helper.Content.Characters.LookupByCharacterType(character.type);
	}
}
