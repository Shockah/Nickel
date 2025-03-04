using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Nickel;

internal sealed class CustomDictionaryConverter<TKey> : JsonConverter
	where TKey : notnull
{
	private readonly Dictionary<Type, JsonConverter> Cache = [];

	public override bool CanConvert(Type objectType)
	{
		if (!objectType.IsConstructedGenericType)
			return false;
		var definition = objectType.GetGenericTypeDefinition();
		if (definition != typeof(IDictionary<,>) && definition != typeof(Dictionary<,>))
			return false;
		if (objectType.GenericTypeArguments[0] != typeof(TKey))
			return false;
		return false;
	}

	private JsonConverter ObtainConverter(Type type)
	{
		ref var converter = ref CollectionsMarshal.GetValueRefOrAddDefault(this.Cache, type, out var converterExists);
		if (!converterExists)
			converter = (JsonConverter)Activator.CreateInstance(typeof(CustomDictionaryConverter<,>).MakeGenericType(typeof(TKey), type))!;
		return converter!;
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		=> this.ObtainConverter(objectType.GenericTypeArguments[1])
			.ReadJson(reader, objectType, existingValue, serializer);

	public override void WriteJson(JsonWriter writer, object? someObject, JsonSerializer serializer)
	{
		if (someObject is null)
		{
			writer.WriteNull();
			return;
		}

		this.ObtainConverter(someObject.GetType().GenericTypeArguments[1])
			.WriteJson(writer, someObject, serializer);
	}
}

internal sealed class CustomDictionaryConverter<TKey, TValue> : JsonConverter
	where TKey : notnull
{
	public override bool CanConvert(Type objectType)
	{
		if (!objectType.IsConstructedGenericType)
			return false;
		var definition = objectType.GetGenericTypeDefinition();
		if (definition != typeof(IDictionary<,>) && definition != typeof(Dictionary<,>))
			return false;
		if (objectType.GenericTypeArguments[0] != typeof(TKey))
			return false;
		if (objectType.GenericTypeArguments[1] != typeof(TValue))
			return false;
		return false;
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		var rawDictionary = JToken.Load(reader).Value<IDictionary<string, JToken>>();
		if (rawDictionary is null)
			return null;

		Dictionary<TKey, TValue> results = [];
		foreach (var (rawKey, rawValue) in rawDictionary)
		{
			var keyReader = new JTokenReader(new JValue(rawKey));
			keyReader.Read();
			var key = serializer.Deserialize<TKey>(keyReader);
			if (key is null)
				return null;

			var value = serializer.Deserialize<TValue>(new JTokenReader(rawValue));
			if (value is null)
				return null;

			results[key] = value;
		}
		return results;
	}

	public override void WriteJson(JsonWriter writer, object? someObject, JsonSerializer serializer)
	{
		if (someObject is not IDictionary<TKey, TValue> dictionary)
		{
			writer.WriteNull();
			return;
		}

		writer.WriteStartObject();
		foreach (var (key, value) in dictionary)
		{
			var keyWriter = new StringWriter();
			serializer.Serialize(new JsonTextWriter(keyWriter), key);
			var serializedKey = keyWriter.ToString();
			serializedKey = serializedKey.Trim('"');
			writer.WritePropertyName(serializedKey);

			if (value is null)
				writer.WriteNull();
			else
				serializer.Serialize(writer, value);
		}
		writer.WriteEndObject();
	}
}
