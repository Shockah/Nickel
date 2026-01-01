namespace Nickel.ModExtensions;

public static class DeckExtensions
{
	extension(Deck deck)
	{
		/// <summary>
		/// The entry for this <see cref="Deck"/>, if it's registered.
		/// </summary>
		public IDeckEntry? Entry
			=> ModExtensions.Helper.Content.Decks.LookupByDeck(deck);
	}
}
