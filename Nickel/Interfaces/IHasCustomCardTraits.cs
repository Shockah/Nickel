using System.Collections.Generic;

namespace Nickel;

public interface IHasCustomCardTraits
{
	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state);
}
