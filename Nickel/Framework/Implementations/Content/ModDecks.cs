using System;

namespace Nickel;

internal sealed class ModDecks(
	IModManifest modManifest,
	Func<DeckManager> deckManagerProvider
) : IModDecks
{
	public IDeckEntry? LookupByDeck(Deck deck)
		=> deckManagerProvider().LookupByDeck(deck);

	public IDeckEntry? LookupByUniqueName(string uniqueName)
		=> deckManagerProvider().LookupByUniqueName(uniqueName);

	public IDeckEntry RegisterDeck(string name, DeckConfiguration configuration)
		=> deckManagerProvider().RegisterDeck(modManifest, name, configuration);
}
