namespace Nickel;

public interface ILocaleBoundLocalizationProvider<TKey>
{
	string? Localize(TKey key, object? tokens = null);
}

public interface ILocaleBoundNonNullLocalizationProvider<TKey> : ILocaleBoundLocalizationProvider<TKey>
{
	new string Localize(TKey key, object? tokens = null);

	string? ILocaleBoundLocalizationProvider<TKey>.Localize(TKey key, object? tokens)
		=> this.Localize(key, tokens);
}
