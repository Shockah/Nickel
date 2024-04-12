using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ModCards : IModCards
{
	private readonly IModManifest ModManifest;
	private readonly Func<CardManager> CardManagerProvider;
	private readonly Func<CardTraitManager> CardTraitManagerProvider;

	public ModCards(IModManifest modManifest, Func<CardManager> cardManagerProvider, Func<CardTraitManager> cardTraitManagerProvider)
	{
		this.ModManifest = modManifest;
		this.CardManagerProvider = cardManagerProvider;
		this.CardTraitManagerProvider = cardTraitManagerProvider;
	}

	public ICardEntry? LookupByCardType(Type cardType)
		=> this.CardManagerProvider().LookupByCardType(cardType);

	public ICardEntry? LookupByUniqueName(string uniqueName)
		=> this.CardManagerProvider().LookupByUniqueName(uniqueName);

	public ICardEntry RegisterCard(CardConfiguration configuration)
		=> this.CardManagerProvider().RegisterCard(this.ModManifest, configuration.CardType.Name, configuration);

	public ICardEntry RegisterCard(string name, CardConfiguration configuration)
		=> this.CardManagerProvider().RegisterCard(this.ModManifest, name, configuration);

	public ICardTraitEntry? LookupTraitByUniqueName(string uniqueName)
		=> this.CardTraitManagerProvider().LookupByUniqueName(uniqueName);

	public ICardTraitEntry RegisterTrait(string name, CardTraitConfiguration configuration)
		=> this.CardTraitManagerProvider().RegisterTrait(this.ModManifest, name, configuration);

	public bool GetCardHasTrait(State state, Card card, ICardTraitEntry trait)
		=> this.CardTraitManagerProvider().GetCardHasTrait(state, card, trait.UniqueName);

	public bool GetCardHasTrait(State state, Card card, string uniqueName)
		=> this.CardTraitManagerProvider().GetCardHasTrait(state, card, uniqueName);

	public IReadOnlySet<ICardTraitEntry> GetCardCurrentTraits(State state, Card card)
		=> this.CardTraitManagerProvider().GetCardCurrentTraits(state, card);

	public void AddCardTraitOverride(Card card, string uniqueName, bool overrideValue, bool isPermanent = false)
		=> this.CardTraitManagerProvider().AddCardTraitOverride(card, uniqueName, overrideValue, isPermanent);
}
