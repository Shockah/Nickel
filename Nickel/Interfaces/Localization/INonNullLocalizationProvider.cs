namespace Nickel;

/// <summary>
/// Describes a localization provider for any phrase, in any locale, that never returns <c>null</c>.
/// </summary>
/// <typeparam name="TKey">The type of keys used to choose the phrase to localize.</typeparam>
public interface INonNullLocalizationProvider<in TKey> : ILocalizationProvider<TKey>
{
	/// <summary>
	/// Localize the phrase for the given key in the given locale.
	/// </summary>
	/// <param name="locale">The locale.</param>
	/// <param name="key">The key for the phrase to localize.</param>
	/// <param name="tokens">Additional tokens to replace in the localized string.</param>
	/// <returns>The localized string.</returns>
	new string Localize(string locale, TKey key, object? tokens = null);

	/// <inheritdoc/>
	string? ILocalizationProvider<TKey>.Localize(string locale, TKey key, object? tokens)
		=> this.Localize(locale, key, tokens);
}
