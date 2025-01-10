using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ModStringEnumConverter<T>(
	Func<string, T> modStringToEnumProvider,
	Func<T, string> modEnumToStringProvider
) : JsonConverter
	where T : struct, Enum
{
	private readonly Dictionary<string, T> StringToEnum = [];
	private readonly Dictionary<T, string> EnumToString = [];
	private bool IsPrepared;

	private void PrepareIfNeeded()
	{
		if (this.IsPrepared)
			return;
		
		foreach (var @enum in Enum.GetValues<T>())
		{
			var name = Enum.GetName(@enum);
			if (string.IsNullOrEmpty(name))
				continue;
			this.StringToEnum[name] = @enum;
			this.EnumToString[@enum] = name;
		}

		this.IsPrepared = true;
	}

	public override bool CanConvert(Type objectType)
		=> objectType == typeof(T) || objectType == typeof(T?);

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		this.PrepareIfNeeded();

		if (reader.TokenType == JsonToken.Null && objectType == typeof(T?))
			return null;
		if (reader.TokenType is not JsonToken.String and not JsonToken.Integer)
			throw new ArgumentException($"Cannot parse {reader.Value} as {typeof(T)}");
		var rawValue = JToken.Load(reader).Value<string>() ?? throw new ArgumentException($"Cannot parse <null> as {typeof(T)}");

		if (this.StringToEnum.TryGetValue(rawValue, out var value))
			return value;

		var modValue = modStringToEnumProvider(rawValue);
		this.StringToEnum[rawValue] = modValue;
		this.EnumToString[modValue] = rawValue;
		return modValue;
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		this.PrepareIfNeeded();

		if (value is not T typedValue)
		{
			writer.WriteNull();
			return;
		}
		if (this.EnumToString.TryGetValue(typedValue, out var rawValue))
		{
			writer.WriteValue(rawValue);
			return;
		}

		rawValue = modEnumToStringProvider(typedValue);
		this.StringToEnum[rawValue] = typedValue;
		this.EnumToString[typedValue] = rawValue;
		writer.WriteValue(rawValue);
	}
}
