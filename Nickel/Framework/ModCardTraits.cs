using System;

namespace Nickel;

internal class ModCardTraits : IModCardTraits
{
	private readonly IModManifest PackageManifest;
	private readonly Func<CardTraitManager> CardTraitManagerProvider;

	public ModCardTraits(IModManifest packageManifest, Func<CardTraitManager> cardTraitManagerProvider)
	{
		this.PackageManifest = packageManifest;
		this.CardTraitManagerProvider = cardTraitManagerProvider;
	}

	public ICardTraitEntry? LookupByUniqueName(string uniqueName) =>
		this.CardTraitManagerProvider().LookupByUniqueName(uniqueName);

	public ICardTraitEntry RegisterTrait(string name, CardTraitConfiguration configuration) =>
		this.CardTraitManagerProvider().RegisterTrait(this.PackageManifest, name, configuration);

	public bool GetCardHasTrait(State state, Card card, ICardTraitEntry trait) =>
		this.CardTraitManagerProvider().GetCardHasTrait(state, card, trait.UniqueName);

	public bool GetCardHasTrait(State state, Card card, string uniqueName) =>
		this.CardTraitManagerProvider().GetCardHasTrait(state, card, uniqueName);

	public void AddCardTraitOverride(Card card, string uniqueName, bool overrideValue, bool isPermanent = false) =>
		this.CardTraitManagerProvider().AddCardTraitOverride(card, uniqueName, overrideValue, isPermanent);
}
