using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ModStringEnumConverter<T> : JsonConverter
	where T : struct, Enum
{
	private Func<string, T> ModStringToEnumProvider { get; }
	private Func<T, string> ModEnumToStringProvider { get; }

	private Dictionary<string, T> StringToEnum { get; } = [];
	private Dictionary<T, string> EnumToString { get; } = [];
	private bool IsPrepared { get; set; } = false;

	public ModStringEnumConverter(
		Func<string, T> modStringToEnumProvider,
		Func<T, string> modEnumToStringProvider
	)
	{
		this.ModStringToEnumProvider = modStringToEnumProvider;
		this.ModEnumToStringProvider = modEnumToStringProvider;
	}

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

		var modValue = this.ModStringToEnumProvider(rawValue);
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

		rawValue = this.ModEnumToStringProvider(typedValue) ?? value.ToString();
		if (rawValue is null)
		{
			writer.WriteNull();
			return;
		}
		this.StringToEnum[rawValue] = typedValue;
		this.EnumToString[typedValue] = rawValue;
		writer.WriteValue(rawValue);
	}
}
