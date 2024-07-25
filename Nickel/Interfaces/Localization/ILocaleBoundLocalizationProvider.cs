namespace Nickel;

/// <summary>
/// Describes a localization provider for any phrase, in a single pre-specified locale.
/// </summary>
/// <typeparam name="TKey">The type of keys used to choose the phrase to localize.</typeparam>
public interface ILocaleBoundLocalizationProvider<in TKey>
{
	/// <summary>
	/// Localize the phrase for the given key in the pre-specified locale.
	/// </summary>
	/// <param name="key">The key for the phrase to localize.</param>
	/// <param name="tokens">Additional tokens to replace in the localized string.</param>
	/// <returns>The localized string, or <c>null</c> if failed.</returns>
	string? Localize(TKey key, object? tokens = null);
}
