using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Nickel;

internal sealed class DictionaryModDataHandler<TRoot>(
	Func<TRoot, Dictionary<string, Dictionary<string, object?>>?> dictionaryGetter,
	Action<TRoot, Dictionary<string, Dictionary<string, object?>>?> dictionarySetter
) : IModDataHandler where TRoot : notnull
{
	public bool CanHandleType(Type type)
		=> type.IsAssignableTo(typeof(TRoot));

	public T GetModData<T>(IModManifest manifest, object o, string key)
	{
		if (!this.CanHandleType(o.GetType()))
			throw new ArgumentException($"Invalid type {o.GetType()} of object {o}");
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
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
		if (!this.CanHandleType(o.GetType()))
			throw new ArgumentException($"Invalid type {o.GetType()} of object {o}");
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
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
		if (!this.CanHandleType(o.GetType()))
			throw new ArgumentException($"Invalid type {o.GetType()} of object {o}");
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
			return false;
		if (!allObjectData.TryGetValue(manifest.UniqueName, out var modObjectData))
			return false;
		return modObjectData.ContainsKey(key);
	}

	public void SetModData<T>(IModManifest manifest, object o, string key, T data)
	{
		if (!this.CanHandleType(o.GetType()))
			throw new ArgumentException($"Invalid type {o.GetType()} of object {o}");
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
		{
			allObjectData = new();
			dictionarySetter((TRoot)o, allObjectData);
		}
		
		ref var modObjectData = ref CollectionsMarshal.GetValueRefOrAddDefault(allObjectData, manifest.UniqueName, out var modObjectDataExists);
		if (!modObjectDataExists)
			modObjectData = [];
		modObjectData![key] = data;
	}

	public void RemoveModData(IModManifest manifest, object o, string key)
	{
		if (!this.CanHandleType(o.GetType()))
			throw new ArgumentException($"Invalid type {o.GetType()} of object {o}");
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
			return;
		
		if (allObjectData.TryGetValue(manifest.UniqueName, out var modObjectData))
		{
			modObjectData.Remove(key);
			if (modObjectData.Count == 0)
				allObjectData.Remove(manifest.UniqueName);
		}
		if (allObjectData.Count == 0)
			dictionarySetter((TRoot)o, null);
	}

	public void CopyOwnedModData(IModManifest manifest, object from, object to)
	{
		if (!this.CanHandleType(from.GetType()))
			throw new ArgumentException($"Invalid type {from.GetType()} of object {from}");
		if (!this.CanHandleType(to.GetType()))
			throw new ArgumentException($"Invalid type {to.GetType()} of object {to}");
		if (dictionaryGetter((TRoot)from) is not { } allSourceObjectData)
			return;
		if (!allSourceObjectData.TryGetValue(manifest.UniqueName, out var sourceModObjectData))
			return;

		if (dictionaryGetter((TRoot)to) is not { } allTargetObjectData)
		{
			allTargetObjectData = [];
			dictionarySetter((TRoot)to, allTargetObjectData);
		}

		ref var targetModObjectData = ref CollectionsMarshal.GetValueRefOrAddDefault(allTargetObjectData, manifest.UniqueName, out var targetModObjectDataExists);
		if (!targetModObjectDataExists)
			targetModObjectData = [];

		foreach (var (key, value) in sourceModObjectData)
			targetModObjectData![key] = NickelStatic.DeepCopyObject(value);
	}

	public void CopyAllModData(object from, object to)
	{
		if (!this.CanHandleType(from.GetType()))
			throw new ArgumentException($"Invalid type {from.GetType()} of object {from}");
		if (!this.CanHandleType(to.GetType()))
			throw new ArgumentException($"Invalid type {to.GetType()} of object {to}");
		if (dictionaryGetter((TRoot)from) is not { } allSourceObjectData)
			return;
		
		if (dictionaryGetter((TRoot)to) is not { } allTargetObjectData)
		{
			allTargetObjectData = [];
			dictionarySetter((TRoot)to, allTargetObjectData);
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
