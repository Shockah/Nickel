using System.Collections.Generic;

namespace Nickel;

public interface IHasCustomCardTraits
{
	public IReadOnlySet<string> GetInnateTraits(State state);
}
