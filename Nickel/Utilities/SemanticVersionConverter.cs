using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Nickel;

/// <summary>
/// A <see cref="JsonConverter"/> capable of (de)serializing <see cref="SemanticVersion"/> values.
/// </summary>
public sealed class SemanticVersionConverter : JsonConverter
{
	/// <inheritdoc />
	public override bool CanConvert(Type objectType)
		=> objectType == typeof(SemanticVersion) || objectType == typeof(SemanticVersion?);

	/// <inheritdoc />
	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		if (objectType == typeof(SemanticVersion?) && reader.TokenType == JsonToken.Null)
			// ReSharper disable once PreferConcreteValueOverDefault
			return default(SemanticVersion?);
		
		if (reader.TokenType != JsonToken.String)
			throw new ArgumentException($"Cannot parse {reader.Value} as {nameof(SemanticVersion)}");
		var rawValue = JToken.Load(reader).Value<string>();
		return SemanticVersionParser.TryParse(rawValue, out var version) ? version : throw new ArgumentException($"Cannot parse {reader.Value} as {nameof(SemanticVersion)}");
	}

	/// <inheritdoc />
	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value is null)
			writer.WriteNull();
		else
			writer.WriteValue(value.ToString());
	}
}
