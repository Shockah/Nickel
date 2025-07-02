using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nickel;

internal sealed class ConditionalWeakTableModDataHandler(
	ConditionalWeakTable<object, Dictionary<string, Dictionary<string, object?>>> storage
) : IModDataHandler
{
	public bool CanHandleType(Type type)
		=> !type.IsValueType;

	public IModDataHandler GetUnderlyingHandler(object o)
		=> this;

	public T GetModData<T>(string modUniqueName, object o, string key){
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!storage.TryGetValue(o, out var allObjectData))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		if (!allObjectData.TryGetValue(modUniqueName, out var modObjectData))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		if (!modObjectData.TryGetValue(key, out var data))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		
		var result = ModDataHandlers.ConvertExtensionData<T>(data, out var reserialized);
		if (reserialized)
			modObjectData[key] = data;
		return result;
	}

	public bool TryGetModData<T>(string modUniqueName, object o, string key, [MaybeNullWhen(false)] out T data)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!storage.TryGetValue(o, out var allObjectData))
		{
			data = default;
			return false;
		}
		if (!allObjectData.TryGetValue(modUniqueName, out var modObjectData))
		{
			data = default;
			return false;
		}
		if (!modObjectData.TryGetValue(key, out var rawData))
		{
			data = default;
			return false;
		}
		
		data = ModDataHandlers.ConvertExtensionData<T>(rawData, out var reserialized);
		if (reserialized)
			modObjectData[key] = data;
		return true;
	}

	public bool ContainsModData(string modUniqueName, object o, string key)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!storage.TryGetValue(o, out var allObjectData))
			return false;
		if (!allObjectData.TryGetValue(modUniqueName, out var modObjectData))
			return false;
		return modObjectData.ContainsKey(key);
	}

	public void SetModData<T>(string modUniqueName, object o, string key, T data)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!storage.TryGetValue(o, out var allObjectData))
		{
			allObjectData = new();
			storage.AddOrUpdate(o, allObjectData);
		}
		
		ref var modObjectData = ref CollectionsMarshal.GetValueRefOrAddDefault(allObjectData, modUniqueName, out var modObjectDataExists);
		if (!modObjectDataExists)
			modObjectData = [];
		modObjectData![key] = data;
	}

	public void RemoveModData(string modUniqueName, object o, string key)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));

		if (!storage.TryGetValue(o, out var allObjectData))
			return;
		
		if (allObjectData.TryGetValue(modUniqueName, out var modObjectData))
		{
			modObjectData.Remove(key);
			if (modObjectData.Count == 0)
				allObjectData.Remove(modUniqueName);
		}
		if (allObjectData.Count == 0)
			storage.Remove(o);
	}

	public bool TryCopyOwnedModDataDirectly(string modUniqueName, object from, object to)
	{
		if (from.GetType().IsValueType)
			return false;
		if (to.GetType().IsValueType)
			return false;
		
		if (!storage.TryGetValue(from, out var allSourceObjectData))
			return true;
		if (!allSourceObjectData.TryGetValue(modUniqueName, out var sourceModObjectData))
			return true;

		if (!storage.TryGetValue(to, out var allTargetObjectData))
		{
			allTargetObjectData = [];
			storage.AddOrUpdate(to, allTargetObjectData);
		}

		ref var targetModObjectData = ref CollectionsMarshal.GetValueRefOrAddDefault(allTargetObjectData, modUniqueName, out var targetModObjectDataExists);
		if (!targetModObjectDataExists)
			targetModObjectData = [];

		foreach (var (key, value) in sourceModObjectData)
			targetModObjectData![key] = NickelStatic.DeepCopyObject(value);
		return true;
	}

	public bool TryCopyAllModDataDirectly(object from, object to)
	{
		if (from.GetType().IsValueType)
			return false;
		if (to.GetType().IsValueType)
			return false;
		
		if (!storage.TryGetValue(from, out var allSourceObjectData))
			return true;
		
		if (!storage.TryGetValue(to, out var allTargetObjectData))
		{
			allTargetObjectData = [];
			storage.AddOrUpdate(to, allTargetObjectData);
		}

		foreach (var (modUniqueName, sourceModObjectData) in allSourceObjectData)
		{
			ref var targetModObjectData = ref CollectionsMarshal.GetValueRefOrAddDefault(allTargetObjectData, modUniqueName, out var targetModObjectDataExists);
			if (!targetModObjectDataExists)
				targetModObjectData = [];
			
			foreach (var (key, value) in sourceModObjectData)
				targetModObjectData![key] = NickelStatic.DeepCopyObject(value);
		}
		return true;
	}

	public bool TryRemoveOwnedModDataDirectly(string modUniqueName, object o)
	{
		if (o.GetType().IsValueType)
			return false;
		if (!storage.TryGetValue(o, out var allObjectData))
			return true;
		
		if (!allObjectData.Remove(modUniqueName))
			return true;
		if (allObjectData.Count == 0)
			storage.Remove(o);
		return true;
	}

	public bool TryRemoveAllModDataDirectly(object o)
	{
		if (o.GetType().IsValueType)
			return false;
		if (!storage.TryGetValue(o, out _))
			return true;
		
		storage.Remove(o);
		return true;
	}

	public IEnumerable<KeyValuePair<string, object?>> GetAllOwnedModData(string modUniqueName, object o)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!storage.TryGetValue(o, out var allObjectData))
			yield break;
		if (!allObjectData.TryGetValue(modUniqueName, out var modObjectData))
			yield break;
		foreach (var kvp in modObjectData)
			yield return kvp;
	}

	public IEnumerable<KeyValuePair<string, IEnumerable<KeyValuePair<string, object?>>>> GetAllModData(object o)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!storage.TryGetValue(o, out var allObjectData))
			yield break;
		foreach (var (key, value) in allObjectData)
			yield return new KeyValuePair<string, IEnumerable<KeyValuePair<string, object?>>>(key, value);
	}
}
