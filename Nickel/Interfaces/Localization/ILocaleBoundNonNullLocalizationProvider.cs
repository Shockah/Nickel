namespace Nickel;

/// <summary>
/// Describes a localization provider for any phrase, in a single pre-specified locale, that never returns <c>null</c>.
/// </summary>
/// <typeparam name="TKey">The type of keys used to choose the phrase to localize.</typeparam>
public interface ILocaleBoundNonNullLocalizationProvider<in TKey> : ILocaleBoundLocalizationProvider<TKey>
{
	/// <summary>
	/// Localize the phrase for the given key in the pre-specified locale.
	/// </summary>
	/// <param name="key">The key for the phrase to localize.</param>
	/// <param name="tokens">Additional tokens to replace in the localized string.</param>
	/// <returns>The localized string.</returns>
	new string Localize(TKey key, object? tokens = null);

	/// <inheritdoc/>
	string? ILocaleBoundLocalizationProvider<TKey>.Localize(TKey key, object? tokens)
		=> this.Localize(key, tokens);
}
