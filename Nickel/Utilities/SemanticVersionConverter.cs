using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Nickel;

/// <summary>
/// A <see cref="JsonConverter"/> capable of (de)serializing of <see cref="SemanticVersion"/> values.
/// </summary>
public sealed class SemanticVersionConverter : JsonConverter<SemanticVersion>
{
	/// <inheritdoc/>
	public override SemanticVersion ReadJson(JsonReader reader, Type objectType, SemanticVersion existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType != JsonToken.String)
			throw new ArgumentException($"Cannot parse {reader.Value} as {nameof(SemanticVersion)}");
		var rawValue = JToken.Load(reader).Value<string>();
		return SemanticVersionParser.TryParse(rawValue, out var version) ? version : throw new ArgumentException($"Cannot parse {reader.Value} as {nameof(SemanticVersion)}");
	}

	/// <inheritdoc/>
	public override void WriteJson(JsonWriter writer, SemanticVersion value, JsonSerializer serializer)
		=> writer.WriteValue(value.ToString());
}
