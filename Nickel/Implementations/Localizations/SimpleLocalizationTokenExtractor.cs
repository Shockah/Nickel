using System.Collections;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// An <see cref="ILocalizationTokenExtractor{TValue}"/> which extracts tokens from either an <see cref="IDictionary{TKey,TValue}"/>, or from an object's properties and fields.
/// </summary>
public sealed class SimpleLocalizationTokenExtractor : ILocalizationTokenExtractor<string>
{
	/// <inheritdoc/>
	public IReadOnlyDictionary<string, string> ExtractTokens(object? @object)
	{
		Dictionary<string, string> results = new();
		if (@object is null)
			return results;

		if (@object is IDictionary dictionary)
		{
			foreach (DictionaryEntry entry in dictionary)
				if (entry.Key?.ToString()?.Trim() is { } key)
					AddResult(key, entry.Value);
		}
		else
		{
			var type = @object.GetType();
			foreach (var field in type.GetFields())
				AddResult(field.Name, field.GetValue(@object));
			foreach (var property in type.GetProperties())
				AddResult(property.Name, property.GetValue(@object));
		}

		return results;

		void AddResult(string key, object? value)
			=> results[key] = value?.ToString() ?? "<null>";
	}
}
