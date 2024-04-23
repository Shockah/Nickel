using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

file static class ObjectExt
{
	public static string ToNiceString(this object? o)
	{
		if (o is IEnumerable<object> enumerable)
			return $"[{string.Join(", ", enumerable.Select(o2 => o2.ToNiceString()))}]";
		return o?.ToString() ?? "<null>";
	}
}

public sealed class MissingPlaceholderLocalizationProvider<TKey>(
	ILocaleBoundLocalizationProvider<TKey> provider,
	Func<TKey, string> missingPlaceholderFunction
) : ILocaleBoundNonNullLocalizationProvider<TKey>
{
	private ILocaleBoundLocalizationProvider<TKey> Provider { get; } = provider;
	private Func<TKey, string> MissingPlaceholderFunction { get; } = missingPlaceholderFunction;

	public MissingPlaceholderLocalizationProvider(ILocaleBoundLocalizationProvider<TKey> provider) : this(provider, key => $"Missing string: `{key.ToNiceString()}`")
	{
	}

	public string Localize(TKey key, object? tokens = null)
		=> this.Provider.Localize(key, tokens) ?? this.MissingPlaceholderFunction(key);
}
