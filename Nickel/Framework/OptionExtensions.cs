using System.CommandLine;
using System.Linq;

namespace Nickel;

internal static class OptionExtensions
{
	extension<T>(Option<T> option)
	{
		public string LongestAlias
			=> option.Aliases.MaxBy(alias => alias.Length)!;
	}
}
