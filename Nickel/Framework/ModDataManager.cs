using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

namespace Nickel;

internal sealed class ModDataManager
{
	internal const string ModDataJsonKey = "ModData";

	internal ConditionalWeakTable<object, Dictionary<string, Dictionary<string, object?>>> ModDataStorage { get; } = [];

	private static T ConvertExtensionData<T>(object? o, out bool reserialized)
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

		var stringWriter = new StringWriter();
		JSONSettings.serializer.Serialize(new JsonTextWriter(stringWriter), o);
		if (JSONSettings.serializer.Deserialize<T>(new JsonTextReader(new StringReader(stringWriter.ToString()))) is { } deserialized)
		{
			reserialized = true;
			return deserialized;
		}

		throw new ArgumentException($"Cannot convert {o} to extension data type {typeof(T)}", nameof(T));
	}

	private static object? DeepCopy(object? o)
	{
		if (o is null)
			return null;

		var type = o.GetType();
		if (type.IsValueType)
			return o;
		
		var stringWriter = new StringWriter();
		JSONSettings.serializer.Serialize(new JsonTextWriter(stringWriter), o);
		if (JSONSettings.serializer.Deserialize(new JsonTextReader(new StringReader(stringWriter.ToString()))) is { } deserialized)
			return deserialized;
		
		throw new ArgumentException($"Cannot deep copy {o}", nameof(o));
	}

	public T GetModData<T>(IModManifest manifest, object o, string key)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!this.ModDataStorage.TryGetValue(o, out var allObjectData))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		if (!allObjectData.TryGetValue(manifest.UniqueName, out var modObjectData))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		if (!modObjectData.TryGetValue(key, out var data))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		
		var result = ConvertExtensionData<T>(data, out var reserialized);
		if (reserialized)
			modObjectData[key] = data;
		return result;
	}

	public bool TryGetModData<T>(IModManifest manifest, object o, string key, [MaybeNullWhen(false)] out T data)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!this.ModDataStorage.TryGetValue(o, out var allObjectData))
		{
			data = default;
			return false;
		}
		if (!allObjectData.TryGetValue(manifest.UniqueName, out var modObjectData))
		{
			data = default;
			return false;
		}
		if (!modObjectData.TryGetValue(key, out var rawData))
		{
			data = default;
			return false;
		}
		
		data = ConvertExtensionData<T>(rawData, out var reserialized);
		if (reserialized)
			modObjectData[key] = data;
		return true;
	}

	public T GetModDataOrDefault<T>(IModManifest manifest, object o, string key, T defaultValue)
		=> this.TryGetModData<T>(manifest, o, key, out var value) ? value : defaultValue;

	public T GetModDataOrDefault<T>(IModManifest manifest, object o, string key) where T : new()
		=> this.TryGetModData<T>(manifest, o, key, out var value) ? value : new();

	public T ObtainModData<T>(IModManifest manifest, object o, string key, Func<T> factory)
	{
		if (!this.TryGetModData<T>(manifest, o, key, out var data))
		{
			data = factory();
			this.SetModData(manifest, o, key, data);
		}
		return data;
	}

	public T ObtainModData<T>(IModManifest manifest, object o, string key) where T : new()
		=> this.ObtainModData(manifest, o, key, () => new T());

	public bool ContainsModData(IModManifest manifest, object o, string key)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!this.ModDataStorage.TryGetValue(o, out var allObjectData))
			return false;
		if (!allObjectData.TryGetValue(manifest.UniqueName, out var modObjectData))
			return false;
		if (!modObjectData.TryGetValue(key, out _))
			return false;
		return true;
	}

	public void SetModData<T>(IModManifest manifest, object o, string key, T data)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!this.ModDataStorage.TryGetValue(o, out var allObjectData))
		{
			allObjectData = new();
			this.ModDataStorage.AddOrUpdate(o, allObjectData);
		}
		if (!allObjectData.TryGetValue(manifest.UniqueName, out var modObjectData))
		{
			modObjectData = new();
			allObjectData[manifest.UniqueName] = modObjectData;
		}
		modObjectData[key] = data;
	}

	public void RemoveModData(IModManifest manifest, object o, string key)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));

		if (!this.ModDataStorage.TryGetValue(o, out var allObjectData))
			return;
		
		if (allObjectData.TryGetValue(manifest.UniqueName, out var modObjectData))
		{
			modObjectData.Remove(key);
			if (modObjectData.Count == 0)
				allObjectData.Remove(manifest.UniqueName);
		}
		if (allObjectData.Count == 0)
			this.ModDataStorage.Remove(o);
	}

	public void CopyOwnedModData(IModManifest manifest, object from, object to)
	{
		if (from.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(from));
		if (to.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(to));
		
		if (!this.ModDataStorage.TryGetValue(from, out var allSourceObjectData))
			return;
		if (!allSourceObjectData.TryGetValue(manifest.UniqueName, out var sourceModObjectData))
			return;

		if (!this.ModDataStorage.TryGetValue(to, out var allTargetObjectData))
		{
			allTargetObjectData = [];
			this.ModDataStorage.AddOrUpdate(to, allTargetObjectData);
		}
		if (!allTargetObjectData.TryGetValue(manifest.UniqueName, out var targetModObjectData))
		{
			targetModObjectData = [];
			allTargetObjectData[manifest.UniqueName] = targetModObjectData;
		}

		foreach (var (key, value) in sourceModObjectData)
			targetModObjectData[key] = DeepCopy(value);
	}

	public void CopyAllModData(object from, object to)
	{
		if (from.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(from));
		if (to.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(to));
		
		if (!this.ModDataStorage.TryGetValue(from, out var allSourceObjectData))
			return;
		
		if (!this.ModDataStorage.TryGetValue(to, out var allTargetObjectData))
		{
			allTargetObjectData = [];
			this.ModDataStorage.AddOrUpdate(to, allTargetObjectData);
		}

		foreach (var (modUniqueName, sourceModObjectData) in allSourceObjectData)
		{
			if (!allTargetObjectData.TryGetValue(modUniqueName, out var targetModObjectData))
			{
				targetModObjectData = [];
				allTargetObjectData[modUniqueName] = targetModObjectData;
			}
			
			foreach (var (key, value) in sourceModObjectData)
				targetModObjectData[key] = DeepCopy(value);
		}
	}
}
