namespace Nickel;

/// <summary>
/// A localization provider that is always bound to the current game locale.
/// </summary>
/// <typeparam name="TKey">The type of keys used to choose the phrase to localize.</typeparam>
/// <param name="provider">An underlying localization provider.</param>
public sealed class CurrentLocaleLocalizationProvider<TKey>(
	ILocalizationProvider<TKey> provider
) : ILocaleBoundLocalizationProvider<TKey>
{
	/// <inheritdoc/>
	public string? Localize(TKey key, object? tokens = null)
		=> provider.Localize(DB.currentLocale.locale, key, tokens);
}
