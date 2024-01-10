using System;

namespace Nickel;

internal sealed class ModCards : IModCards
{
	private IModManifest ModManifest { get; }
	private Func<CardManager> CardManagerProvider { get; }

	public ModCards(IModManifest modManifest, Func<CardManager> cardManagerProvider)
	{
		this.ModManifest = modManifest;
		this.CardManagerProvider = cardManagerProvider;
	}

	public ICardEntry? LookupByCardType(Type cardType)
		=> this.CardManagerProvider().LookupByCardType(cardType);

	public ICardEntry? LookupByUniqueName(string uniqueName)
		=> this.CardManagerProvider().LookupByUniqueName(uniqueName);

	public ICardEntry RegisterCard(string name, CardConfiguration configuration)
		=> this.CardManagerProvider().RegisterCard(this.ModManifest, name, configuration);
}
