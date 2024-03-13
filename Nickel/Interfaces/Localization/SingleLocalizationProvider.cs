namespace Nickel;

/// <summary>
/// Describes a localization provider for a single pre-specified phrase, in any locale.
/// </summary>
public delegate string? SingleLocalizationProvider(string locale);

/// <summary>
/// Hosts extensions for localization providers.
/// </summary>
public static class LocalizationProviderExt
{
	/// <summary>
	/// Localize the phrase in the given locale.
	/// </summary>
	/// <param name="provider">The localization provider.</param>
	/// <param name="locale">The locale.</param>
	/// <returns>The localized string, or <c>null</c> if failed.</returns>
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
