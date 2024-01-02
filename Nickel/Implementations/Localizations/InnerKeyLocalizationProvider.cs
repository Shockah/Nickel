using System.Collections.Generic;
using System.Linq;

namespace Nickel;

public sealed class InnerKeyLocalizationProvider(
	ILocalizationProvider<IReadOnlyList<string>> provider,
	IReadOnlyList<string> key
) : ILocalizationProvider<IReadOnlyList<string>>
{
	private ILocalizationProvider<IReadOnlyList<string>> Provider { get; } = provider;
	private IReadOnlyList<string> Key { get; } = key;

	public string? Localize(string locale, IReadOnlyList<string> key, object? tokens = null)
		=> this.Provider.Localize(locale, this.Key.Concat(key).ToList(), tokens);
}
