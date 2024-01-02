namespace Nickel;

public interface ILocalizationProvider<TKey>
{
	string? Localize(string locale, TKey key, object? tokens = null);
}
