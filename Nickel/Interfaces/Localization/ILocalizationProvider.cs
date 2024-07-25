namespace Nickel;

/// <summary>
/// Describes a localization provider for any phrase, in any locale.
/// </summary>
/// <typeparam name="TKey">The type of keys used to choose the phrase to localize.</typeparam>
public interface ILocalizationProvider<in TKey>
{
	/// <summary>
	/// Localize the phrase for the given key in the given locale.
	/// </summary>
	/// <param name="locale">The locale.</param>
	/// <param name="key">The key for the phrase to localize.</param>
	/// <param name="tokens">Additional tokens to replace in the localized string.</param>
	/// <returns>The localized string, or <c>null</c> if failed.</returns>
	string? Localize(string locale, TKey key, object? tokens = null);
}

/// <summary>
/// Hosts extensions for localization providers.
/// </summary>
public static class ILocalizationProviderExt
{
	/// <summary>
	/// Create a localization provider that is bound to the provided key and set of additional tokens. 
	/// </summary>
	/// <typeparam name="TKey">The type of keys used to choose the phrase to localize.</typeparam>
	/// <param name="self">The localization provider.</param>
	/// <param name="key">The key for the phrase to localize.</param>
	/// <param name="tokens">Additional tokens to replace in the localized string.</param>
	/// <returns></returns>
	public static IKeyAndTokensBoundLocalizationProvider Bind<TKey>(this ILocalizationProvider<TKey> self, TKey key, object? tokens = null)
		=> new KeyAndTokensBoundLocalizationProvider<TKey>(self, key, tokens);
}
