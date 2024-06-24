using System;

namespace Nickel.Essentials;

public interface IEssentialsApi
{
	Type? GetExeCardTypeForDeck(Deck deck);
	Deck? GetDeckForExeCardType(Type type);
	bool IsExeCardType(Type type);
}
