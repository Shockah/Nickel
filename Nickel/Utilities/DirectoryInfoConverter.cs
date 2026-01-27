using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Nickel;

/// <summary>
/// A <see cref="JsonConverter"/> capable of (de)serializing of <see cref="DirectoryInfo"/> values.
/// </summary>
public sealed class DirectoryInfoConverter : JsonConverter<DirectoryInfo>
{
	/// <inheritdoc/>
	public override DirectoryInfo? ReadJson(JsonReader reader, Type objectType, DirectoryInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
			return null;
		if (reader.TokenType != JsonToken.String)
			throw new ArgumentException($"Cannot parse {reader.Value} as {nameof(DirectoryInfo)}");
		var rawValue = JToken.Load(reader).Value<string>();
		return rawValue is null ? null : new DirectoryInfo(rawValue);
	}

	/// <inheritdoc/>
	public override void WriteJson(JsonWriter writer, DirectoryInfo? value, JsonSerializer serializer)
	{
		if (value is null)
			writer.WriteNull();
		else
			writer.WriteValue(value.ToString());
	}
}
