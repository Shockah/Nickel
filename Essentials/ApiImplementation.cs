using System;

namespace Nickel.Essentials;

public sealed class ApiImplementation : IEssentialsApi
{
	public Type? GetExeCardTypeForDeck(Deck deck)
		=> ModEntry.Instance.GetExeType(deck);

	public Deck? GetDeckForExeCardType(Type type)
		=> NewRunOptions.allChars.FirstOrNull(deck => this.GetExeCardTypeForDeck(deck) == type);

	public bool IsExeCardType(Type type)
		=> this.GetDeckForExeCardType(type) is not null;
}
