namespace Nickel;

public sealed class CurrentLocaleOrEnglishLocalizationProvider<TKey>(
	ILocalizationProvider<TKey> provider
) : ILocaleBoundLocalizationProvider<TKey>
{
	private ILocalizationProvider<TKey> Provider { get; } = provider;

	public string? Localize(TKey key, object? tokens = null)
	{
		if (this.Provider.Localize(DB.currentLocale.locale, key, tokens) is { } currentLocalized)
			return currentLocalized;
		if (DB.currentLocale.locale != "en" && this.Provider.Localize("en", key, tokens) is { } englishLocalized)
			return englishLocalized;
		return null;
	}
}
