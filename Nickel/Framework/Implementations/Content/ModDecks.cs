using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ModDecks(
	IModManifest modManifest,
	Func<DeckManager> deckManagerProvider
) : IModDecks
{
	public IReadOnlyDictionary<string, IDeckEntry> RegisteredDecks
		=> this.RegisteredDeckStorage;
	
	private readonly Dictionary<string, IDeckEntry> RegisteredDeckStorage = [];
	
	public IDeckEntry? LookupByDeck(Deck deck)
		=> deckManagerProvider().LookupByDeck(deck);

	public IDeckEntry? LookupByUniqueName(string uniqueName)
		=> deckManagerProvider().LookupByUniqueName(uniqueName);

	public IDeckEntry RegisterDeck(string name, DeckConfiguration configuration)
	{
		var entry = deckManagerProvider().RegisterDeck(modManifest, name, configuration);
		this.RegisteredDeckStorage[name] = entry;
		return entry;
	}
}
