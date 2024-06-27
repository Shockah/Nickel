using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nickel;

/// <summary>
/// A localization provider which returns data from JSON files.
/// </summary>
/// <param name="tokenExtractor">A token extractor to use for replacing tokens in localized strings.</param>
/// <param name="localeStreamFunction">A function that returns a <see cref="Stream"/> to a JSON file for the given locale.</param>
/// <param name="serializer">The JSON serializer to use for deserializing.</param>
public sealed partial class JsonLocalizationProvider(
	ILocalizationTokenExtractor<string> tokenExtractor,
	Func<string, Stream?> localeStreamFunction,
	JsonSerializer? serializer = null
) : ILocalizationProvider<IReadOnlyList<string>>
{
	private readonly JsonSerializer Serializer = serializer ?? new();

	private readonly Dictionary<string, JObject?> LocalizationCache = [];

	/// <inheritdoc/>
	public string? Localize(string locale, IReadOnlyList<string> key, object? tokens = null)
	{
		if (this.GetLocalization(locale) is not { } localization)
			return null;
		return this.Localize(localization, key, 0, tokens);
	}

	private JObject? GetLocalization(string locale)
	{
		if (this.LocalizationCache.TryGetValue(locale, out var localization))
			return localization;

		try
		{
			using var stream = localeStreamFunction(locale);
			if (stream is null)
				return null;
		
			using var streamReader = new StreamReader(stream);
			using var jsonReader = new JsonTextReader(streamReader);

			localization = this.Serializer.Deserialize<JObject>(jsonReader) ?? new JObject();
			this.LocalizationCache[locale] = localization;
			return localization;
		}
		catch
		{
			this.LocalizationCache[locale] = null;
			return localization;
		}
	}

	private string? Localize(JToken localization, IReadOnlyList<string> key, int keyIndex, object? tokens)
	{
		if (keyIndex >= key.Count)
		{
			if (localization is JValue value && value.Value<string>() is { } localizationString)
				return this.Localize(localizationString, tokens);
			else if (localization is JArray array)
				return this.Localize(string.Join("\n", array.Select(v => v.Value<string>()).OfType<string>()), tokens);
			else
				return null;
		}
		else
		{
			if (localization is JObject @object)
				return this.Localize(@object, key, keyIndex, tokens);
			if (localization is JArray array)
				return this.Localize(array, key, keyIndex, tokens);
			return null;
		}
	}

	private string? Localize(JObject localization, IReadOnlyList<string> key, int keyIndex, object? tokens)
		=> localization.GetValue(key[keyIndex]) is { } token ? this.Localize(token, key, keyIndex + 1, tokens) : null;

	private string? Localize(JArray localization, IReadOnlyList<string> key, int keyIndex, object? tokens)
		=> int.TryParse(key[keyIndex], out var arrayIndex) && localization.Count >= arrayIndex ? this.Localize(localization[arrayIndex], key, keyIndex + 1, tokens) : null;

	private string Localize(string localizationString, object? tokens)
	{
		var tokenLookup = tokenExtractor.ExtractTokens(tokens);
		return TokenRegex().Replace(localizationString, match =>
		{
			var key = match.Groups[1].Value.Trim();
			return tokenLookup.TryGetValue(key, out var value) ? value : match.Value;
		});
	}

	[GeneratedRegex(@"{{([ \w\.\-]+)}}")]
	private static partial Regex TokenRegex();
}
