using System;

namespace Nickel;

internal sealed class ModDecks : IModDecks
{
	private IModManifest ModManifest { get; }
	private Func<DeckManager> DeckManagerProvider { get; }

	public ModDecks(IModManifest modManifest, Func<DeckManager> deckManagerProvider)
	{
		this.ModManifest = modManifest;
		this.DeckManagerProvider = deckManagerProvider;
	}

	public IDeckEntry RegisterDeck(string name, DeckConfiguration configuration)
		=> this.DeckManagerProvider().RegisterDeck(this.ModManifest, name, configuration);
}
