using System.Collections.Generic;

namespace Nickel;

/// <seealso cref="IModEvents.OnLoadStringsForLocale"/>
public readonly struct LoadStringsForLocaleEventArgs
{
	/// <summary>The locale currently being set up.</summary>
	public required string Locale { get; init; }
	
	/// <summary>The localizations for the locale currently being set up.</summary>
	public required Dictionary<string, string> Localizations { get; init; }
}
