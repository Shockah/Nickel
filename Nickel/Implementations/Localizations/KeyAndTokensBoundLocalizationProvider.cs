namespace Nickel;

/// <summary>
/// An <see cref="IKeyAndTokensBoundLocalizationProvider"/> implementation using localizations coming from an <see cref="ILocalizationProvider{TKey}"/>.
/// </summary>
/// <typeparam name="TKey">The type of keys used to choose the phrase to localize.</typeparam>
/// <param name="provider">The provider to take localizations from.</param>
/// <param name="key">The key for the phrase to localize.</param>
/// <param name="tokens">Additional tokens to replace in the localized string.</param>
public sealed class KeyAndTokensBoundLocalizationProvider<TKey>(
	ILocalizationProvider<TKey> provider,
	TKey key,
	object? tokens
) : IKeyAndTokensBoundLocalizationProvider
{
	/// <inheritdoc/>
	public string? Localize(string locale)
		=> provider.Localize(locale, key, tokens);
}
