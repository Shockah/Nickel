using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

/// <summary>
/// A localization provider which returns a placeholder (like <c>Missing string</c>) for any localizations it could not handle.
/// </summary>
/// <typeparam name="TKey">The type of keys used to choose the phrase to localize.</typeparam>
/// <param name="provider">An underlying localization provider.</param>
/// <param name="missingPlaceholderFunction">A function that returns the placeholder.</param>
public sealed class MissingPlaceholderNonBoundLocalizationProvider<TKey>(
	ILocalizationProvider<TKey> provider,
	Func<TKey, string> missingPlaceholderFunction
) : INonNullLocalizationProvider<TKey>
{
	/// <summary>
	/// Creates a localization provider which returns the <c>Missing string</c> placeholder for any localizations it could not handle.
	/// </summary>
	/// <param name="provider">An underlying localization provider.</param>
	public MissingPlaceholderNonBoundLocalizationProvider(ILocalizationProvider<TKey> provider) : this(provider, key => $"Missing string: `{ToNiceString(key)}`")
	{
	}

	/// <inheritdoc cref="ILocaleBoundNonNullLocalizationProvider{TKey}.Localize"/>
	public string Localize(string locale, TKey key, object? tokens = null)
		=> provider.Localize(locale, key, tokens) ?? missingPlaceholderFunction(key);

	private static string ToNiceString(object? o)
	{
		if (o is IEnumerable<object> enumerable)
			return $"[{string.Join(", ", enumerable.Select(ToNiceString))}]";
		return o?.ToString() ?? "<null>";
	}
}
