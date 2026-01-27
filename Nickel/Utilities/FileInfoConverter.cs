using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Nickel;

/// <summary>
/// A <see cref="JsonConverter"/> capable of (de)serializing of <see cref="FileInfo"/> values.
/// </summary>
public sealed class FileInfoConverter : JsonConverter<FileInfo>
{
	/// <inheritdoc/>
	public override FileInfo? ReadJson(JsonReader reader, Type objectType, FileInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
			return null;
		if (reader.TokenType != JsonToken.String)
			throw new ArgumentException($"Cannot parse {reader.Value} as {nameof(FileInfo)}");
		var rawValue = JToken.Load(reader).Value<string>();
		return rawValue is null ? null : new FileInfo(rawValue);
	}

	/// <inheritdoc/>
	public override void WriteJson(JsonWriter writer, FileInfo? value, JsonSerializer serializer)
	{
		if (value is null)
			writer.WriteNull();
		else
			writer.WriteValue(value.ToString());
	}
}
