using System.Collections;
using System.Collections.Generic;

namespace Nickel;

public sealed class SimpleLocalizationTokenExtractor : ILocalizationTokenExtractor<string>
{
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
