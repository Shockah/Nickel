using System;
using System.Collections.Generic;
using System.Linq;

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

internal sealed class VanillaModDecks(
	Func<DeckManager> deckManagerProvider
) : IModDecks
{
	private readonly Lazy<Dictionary<string, IDeckEntry>> LazyRegisteredDecks = new(() => Enum.GetValues<Deck>().Select(d => deckManagerProvider().LookupByDeck(d)!).ToDictionary(e => e.Deck.Key()));
	
	public IReadOnlyDictionary<string, IDeckEntry> RegisteredDecks
		=> this.LazyRegisteredDecks.Value;
	
	public IDeckEntry? LookupByDeck(Deck deck)
		=> deckManagerProvider().LookupByDeck(deck);

	public IDeckEntry? LookupByUniqueName(string uniqueName)
		=> deckManagerProvider().LookupByUniqueName(uniqueName);

	public IDeckEntry RegisterDeck(string name, DeckConfiguration configuration)
		=> throw new NotSupportedException();
}
