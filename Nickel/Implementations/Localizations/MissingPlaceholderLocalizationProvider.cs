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

/// <summary>
/// A localization provider which returns a placeholder (like <c>Missing string</c>) for any localizations it could not handle.
/// </summary>
/// <typeparam name="TKey">The type of keys used to choose the phrase to localize.</typeparam>
/// <param name="provider">An underlying localization provider.</param>
/// <param name="missingPlaceholderFunction">A function that returns the placeholder.</param>
public sealed class MissingPlaceholderLocalizationProvider<TKey>(
	ILocaleBoundLocalizationProvider<TKey> provider,
	Func<TKey, string> missingPlaceholderFunction
) : ILocaleBoundNonNullLocalizationProvider<TKey>
{
	private ILocaleBoundLocalizationProvider<TKey> Provider { get; } = provider;
	private Func<TKey, string> MissingPlaceholderFunction { get; } = missingPlaceholderFunction;

	/// <summary>
	/// Creates a localization provider which returns the <c>Missing string</c> placeholder for any localizations it could not handle.
	/// </summary>
	/// <param name="provider">An underlying localization provider.</param>
	public MissingPlaceholderLocalizationProvider(ILocaleBoundLocalizationProvider<TKey> provider) : this(provider, key => $"Missing string: `{key.ToNiceString()}`")
	{
	}

	/// <inheritdoc cref="ILocaleBoundNonNullLocalizationProvider{TKey}.Localize"/>
	public string Localize(TKey key, object? tokens = null)
		=> this.Provider.Localize(key, tokens) ?? this.MissingPlaceholderFunction(key);
}
