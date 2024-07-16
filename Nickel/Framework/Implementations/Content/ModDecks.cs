using System;

namespace Nickel;

internal sealed class ModDecks : IModDecks
{
	private readonly IModManifest ModManifest;
	private readonly Func<DeckManager> DeckManagerProvider;

	public ModDecks(IModManifest modManifest, Func<DeckManager> deckManagerProvider)
	{
		this.ModManifest = modManifest;
		this.DeckManagerProvider = deckManagerProvider;
	}

	public IDeckEntry? LookupByDeck(Deck deck)
		=> this.DeckManagerProvider().LookupByDeck(deck);

	public IDeckEntry? LookupByUniqueName(string uniqueName)
		=> this.DeckManagerProvider().LookupByUniqueName(uniqueName);

	public IDeckEntry RegisterDeck(string name, DeckConfiguration configuration)
		=> this.DeckManagerProvider().RegisterDeck(this.ModManifest, name, configuration);
}
