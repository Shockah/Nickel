namespace Nickel;

public delegate string? LocalizationProvider(string locale);

public static class LocalizationProviderExt
{
	public static string? Localize(this LocalizationProvider? provider, string locale)
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
