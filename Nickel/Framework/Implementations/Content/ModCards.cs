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

	public ICardEntry RegisterCard(string name, CardConfiguration configuration)
		=> this.CardManagerProvider().RegisterCard(this.ModManifest, name, configuration);
}
