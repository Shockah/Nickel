using Newtonsoft.Json;
using System;
using System.IO;

namespace Nickel;

internal static class ModDataHandlers
{
	internal const string ModDataJsonKey = "ModData";
	
	internal static T ConvertExtensionData<T>(object? o, out bool reserialized)
	{
		reserialized = false;
		if (o is T t)
			return t;
		if (typeof(T) == typeof(int))
			return (T)(object)Convert.ToInt32(o);
		if (typeof(T) == typeof(long))
			return (T)(object)Convert.ToInt64(o);
		if (typeof(T) == typeof(short))
			return (T)(object)Convert.ToInt16(o);
		if (typeof(T) == typeof(byte))
			return (T)(object)Convert.ToByte(o);
		if (typeof(T) == typeof(bool))
			return (T)(object)Convert.ToBoolean(o);
		if (typeof(T) == typeof(float))
			return (T)(object)Convert.ToSingle(o);
		if (typeof(T) == typeof(double))
			return (T)(object)Convert.ToDouble(o);
		if (typeof(T) == typeof(uint))
			return (T)(object)Convert.ToUInt32(o);
		if (typeof(T) == typeof(ulong))
			return (T)(object)Convert.ToUInt64(o);
		if (typeof(T) == typeof(ushort))
			return (T)(object)Convert.ToUInt16(o);
		if (typeof(T) == typeof(sbyte))
			return (T)(object)Convert.ToSByte(o);
		if (o is null && (!typeof(T).IsValueType || (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))))
			return default!;

		if (o is null)
			throw new ArgumentException($"Cannot convert <null> to extension data type {typeof(T)}", nameof(T));
		
		var stringWriter = new StringWriter();
		JSONSettings.serializer.Serialize(new JsonTextWriter(stringWriter), o);
		if (JSONSettings.serializer.Deserialize<T>(new JsonTextReader(new StringReader(stringWriter.ToString()))) is { } deserialized)
		{
			reserialized = true;
			return deserialized;
		}
		
		throw new ArgumentException($"Cannot convert {o} of type {o.GetType()} to extension data type {typeof(T)}", nameof(T));
	}
}
