using System;

namespace Nickel;

public interface IModCards
{
	ICardEntry? LookupByCardType(Type cardType);
	ICardEntry? LookupByUniqueName(string uniqueName);
	ICardEntry RegisterCard(CardConfiguration configuration);
	ICardEntry RegisterCard(string name, CardConfiguration configuration);
}
