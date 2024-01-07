namespace Nickel;

public sealed class KeyAndTokensBoundLocalizationProvider<TKey>(
	ILocalizationProvider<TKey> provider,
	TKey key,
	object? tokens
) : IKeyAndTokensBoundLocalizationProvider
{
	private ILocalizationProvider<TKey> Provider { get; } = provider;
	private TKey Key { get; } = key;
	private object? Tokens { get; } = tokens;

	public string? Localize(string locale)
		=> this.Provider.Localize(locale, this.Key, this.Tokens);
}
