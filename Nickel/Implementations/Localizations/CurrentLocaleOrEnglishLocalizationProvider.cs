namespace Nickel;

/// <summary>
/// A localization provider that is always bound to the current game locale, but defaults to English if a localization is not found.
/// </summary>
/// <typeparam name="TKey">The type of keys used to choose the phrase to localize.</typeparam>
/// <param name="provider">An underlying localization provider.</param>
public sealed class CurrentLocaleOrEnglishLocalizationProvider<TKey>(
	ILocalizationProvider<TKey> provider
) : ILocaleBoundLocalizationProvider<TKey>
{
	private ILocalizationProvider<TKey> Provider { get; } = provider;

	/// <inheritdoc/>
	public string? Localize(TKey key, object? tokens = null)
	{
		if (this.Provider.Localize(DB.currentLocale.locale, key, tokens) is { } currentLocalized)
			return currentLocalized;
		if (DB.currentLocale.locale != "en" && this.Provider.Localize("en", key, tokens) is { } englishLocalized)
			return englishLocalized;
		return null;
	}
}
