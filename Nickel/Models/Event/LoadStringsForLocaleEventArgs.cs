using System.Collections.Generic;

namespace Nickel;

public readonly struct LoadStringsForLocaleEventArgs
{
	public required string Locale { get; init; }
	public required Dictionary<string, string> Localizations { get; init; }
}
