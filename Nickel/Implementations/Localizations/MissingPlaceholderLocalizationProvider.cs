using System;

namespace Nickel;

public sealed class MissingPlaceholderLocalizationProvider<TKey>(
	ILocaleBoundLocalizationProvider<TKey> provider,
	Func<TKey, string> missingPlaceholderFunction
) : ILocaleBoundNonNullLocalizationProvider<TKey>
{
	private ILocaleBoundLocalizationProvider<TKey> Provider { get; } = provider;
	private Func<TKey, string> MissingPlaceholderFunction { get; } = missingPlaceholderFunction;

	public MissingPlaceholderLocalizationProvider(ILocaleBoundLocalizationProvider<TKey> provider) : this(provider, key => $"Missing string: `{key}`")
	{
	}

	public string Localize(TKey key, object? tokens = null)
		=> this.Provider.Localize(key, tokens) ?? this.MissingPlaceholderFunction(key);
}
