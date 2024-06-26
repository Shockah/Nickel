using Newtonsoft.Json;
using System;

namespace Nickel;

/// <summary>
/// A <see cref="JsonConverter"/> which specifies a concrete type to deserialize to. This can be used to hide the concrete type behind an interface.
/// </summary>
/// <typeparam name="TConcrete">The concrete type to deserialize to.</typeparam>
public sealed class ConcreteTypeConverter<TConcrete> : JsonConverter
{
	/// <inheritdoc/>
	public override bool CanConvert(Type objectType)
		=> true;

	/// <inheritdoc/>
	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		=> serializer.Deserialize<TConcrete>(reader);

	/// <inheritdoc/>
	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		=> serializer.Serialize(writer, value);
}
