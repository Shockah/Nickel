using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nickel.Common;
using System;

namespace Nickel;

public sealed class SemanticVersionConverter : JsonConverter<SemanticVersion>
{
	public override SemanticVersion ReadJson(JsonReader reader, Type objectType, SemanticVersion existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType != JsonToken.String)
			throw new ArgumentException($"Cannot parse {reader.Value} as {nameof(SemanticVersion)}.");
		var rawValue = JToken.Load(reader).Value<string>();
		return SemanticVersionParser.TryParse(rawValue, out var version) ? version : throw new ArgumentException($"Cannot parse {reader.Value} as {nameof(SemanticVersion)}.");
	}

	public override void WriteJson(JsonWriter writer, SemanticVersion value, JsonSerializer serializer)
		=> writer.WriteValue(value.ToString());
}
