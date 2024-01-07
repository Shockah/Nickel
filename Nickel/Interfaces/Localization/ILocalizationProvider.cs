namespace Nickel;

public interface ILocalizationProvider<TKey>
{
	string? Localize(string locale, TKey key, object? tokens = null);
}

public static class ILocalizationProviderExt
{
	public static IKeyAndTokensBoundLocalizationProvider Bind<TKey>(this ILocalizationProvider<TKey> self, TKey key, object? tokens = null)
		=> new KeyAndTokensBoundLocalizationProvider<TKey>(self, key, tokens);
}
