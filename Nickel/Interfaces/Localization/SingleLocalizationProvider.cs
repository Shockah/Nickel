namespace Nickel;

public delegate string? SingleLocalizationProvider(string locale);

public static class LocalizationProviderExt
{
	public static string? Localize(this SingleLocalizationProvider? provider, string locale)
	{
		if (provider is null)
			return null;
		if (provider(locale) is { } localized)
			return localized;
		if (locale != "en" && provider("en") is { } englishLocalized)
			return englishLocalized;
		return null;
	}
}
