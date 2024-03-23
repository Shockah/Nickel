using System;

namespace Nickel;

/// <summary>
/// A mod-specific card registry.
/// Allows looking up and registering cards.
/// </summary>
public interface IModCards
{
	ICardEntry? LookupByCardType(Type cardType);
	ICardEntry? LookupByUniqueName(string uniqueName);
	ICardEntry RegisterCard(CardConfiguration configuration);
	ICardEntry RegisterCard(string name, CardConfiguration configuration);
}
