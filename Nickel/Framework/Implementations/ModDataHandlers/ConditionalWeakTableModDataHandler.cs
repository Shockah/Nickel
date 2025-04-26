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
	
	public T GetModData<T>(IModManifest manifest, object o, string key){
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!storage.TryGetValue(o, out var allObjectData))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		if (!allObjectData.TryGetValue(manifest.UniqueName, out var modObjectData))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		if (!modObjectData.TryGetValue(key, out var data))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		
		var result = ModDataHandlers.ConvertExtensionData<T>(data, out var reserialized);
		if (reserialized)
			modObjectData[key] = data;
		return result;
	}

	public bool TryGetModData<T>(IModManifest manifest, object o, string key, [MaybeNullWhen(false)] out T data)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!storage.TryGetValue(o, out var allObjectData))
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
		
		data = ModDataHandlers.ConvertExtensionData<T>(rawData, out var reserialized);
		if (reserialized)
			modObjectData[key] = data;
		return true;
	}

	public bool ContainsModData(IModManifest manifest, object o, string key)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!storage.TryGetValue(o, out var allObjectData))
			return false;
		if (!allObjectData.TryGetValue(manifest.UniqueName, out var modObjectData))
			return false;
		return modObjectData.ContainsKey(key);
	}

	public void SetModData<T>(IModManifest manifest, object o, string key, T data)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));
		if (!storage.TryGetValue(o, out var allObjectData))
		{
			allObjectData = new();
			storage.AddOrUpdate(o, allObjectData);
		}
		
		ref var modObjectData = ref CollectionsMarshal.GetValueRefOrAddDefault(allObjectData, manifest.UniqueName, out var modObjectDataExists);
		if (!modObjectDataExists)
			modObjectData = [];
		modObjectData![key] = data;
	}

	public void RemoveModData(IModManifest manifest, object o, string key)
	{
		if (o.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(o));

		if (!storage.TryGetValue(o, out var allObjectData))
			return;
		
		if (allObjectData.TryGetValue(manifest.UniqueName, out var modObjectData))
		{
			modObjectData.Remove(key);
			if (modObjectData.Count == 0)
				allObjectData.Remove(manifest.UniqueName);
		}
		if (allObjectData.Count == 0)
			storage.Remove(o);
	}

	public void CopyOwnedModData(IModManifest manifest, object from, object to)
	{
		if (from.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(from));
		if (to.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(to));
		
		if (!storage.TryGetValue(from, out var allSourceObjectData))
			return;
		if (!allSourceObjectData.TryGetValue(manifest.UniqueName, out var sourceModObjectData))
			return;

		if (!storage.TryGetValue(to, out var allTargetObjectData))
		{
			allTargetObjectData = [];
			storage.AddOrUpdate(to, allTargetObjectData);
		}

		ref var targetModObjectData = ref CollectionsMarshal.GetValueRefOrAddDefault(allTargetObjectData, manifest.UniqueName, out var targetModObjectDataExists);
		if (!targetModObjectDataExists)
			targetModObjectData = [];

		foreach (var (key, value) in sourceModObjectData)
			targetModObjectData![key] = NickelStatic.DeepCopyObject(value);
	}

	public void CopyAllModData(object from, object to)
	{
		if (from.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(from));
		if (to.GetType().IsValueType)
			throw new ArgumentException("Mod data can only be put on reference (class) types", nameof(to));
		
		if (!storage.TryGetValue(from, out var allSourceObjectData))
			return;
		
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
	}
}
