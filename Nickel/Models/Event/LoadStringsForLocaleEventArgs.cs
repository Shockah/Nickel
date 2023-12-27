using System.Collections.Generic;

namespace Nickel;

public readonly struct LoadStringsForLocaleEventArgs
{
    public string Locale { get; init; }
    public Dictionary<string, string> Localizations { get; init; }
}
