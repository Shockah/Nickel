namespace Nickel;

/// <summary>
/// A localization provider for any locale, but which defaults to English if a localization is not found.
/// </summary>
/// <typeparam name="TKey">The type of keys used to choose the phrase to localize.</typeparam>
/// <param name="provider">An underlying localization provider.</param>
public sealed class EnglishFallbackLocalizationProvider<TKey>(
	ILocalizationProvider<TKey> provider
) : ILocalizationProvider<TKey>
{
	/// <inheritdoc/>
	public string? Localize(string locale, TKey key, object? tokens = null)
	{
		if (provider.Localize(locale, key, tokens) is { } localized)
			return localized;
		if (locale != "en" && provider.Localize("en", key, tokens) is { } englishLocalized)
			return englishLocalized;
		return null;
	}
}
