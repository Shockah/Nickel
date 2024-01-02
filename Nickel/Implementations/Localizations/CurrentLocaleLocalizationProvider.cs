namespace Nickel;

public sealed class CurrentLocaleLocalizationProvider<TKey>(
	ILocalizationProvider<TKey> provider
) : ILocaleBoundLocalizationProvider<TKey>
{
	private ILocalizationProvider<TKey> Provider { get; } = provider;

	public string? Localize(TKey key, object? tokens = null)
		=> this.Provider.Localize(DB.currentLocale.locale, key, tokens);
}
