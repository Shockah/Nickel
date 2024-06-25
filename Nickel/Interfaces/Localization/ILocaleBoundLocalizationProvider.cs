namespace Nickel;

public interface ILocaleBoundLocalizationProvider<in TKey>
{
	string? Localize(TKey key, object? tokens = null);
}

public interface ILocaleBoundNonNullLocalizationProvider<in TKey> : ILocaleBoundLocalizationProvider<TKey>
{
	new string Localize(TKey key, object? tokens = null);

	string? ILocaleBoundLocalizationProvider<TKey>.Localize(TKey key, object? tokens)
		=> this.Localize(key, tokens);
}
