using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Allows extracting tokens from provided objects, to replace in localized strings.
/// </summary>
/// <typeparam name="TValue">The type of values to extract.</typeparam>
public interface ILocalizationTokenExtractor<TValue>
{
	/// <summary>
	/// Extracts tokens from the provided object, to replace in localized strings.
	/// </summary>
	/// <param name="object">The object to extract tokens from.</param>
	/// <returns>A dictionary of tokens to replace in localized strings.</returns>
	IReadOnlyDictionary<string, TValue> ExtractTokens(object? @object);
}
