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

	public IModDataHandler GetUnderlyingHandler(object o)
		=> this;

	public T GetModData<T>(string modUniqueName, object o, string key)
	{
		if (!this.CanHandleType(o.GetType()))
			throw new ArgumentException($"Invalid type {o.GetType()} of object {o}");
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
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
		if (!this.CanHandleType(o.GetType()))
			throw new ArgumentException($"Invalid type {o.GetType()} of object {o}");
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
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
		if (!this.CanHandleType(o.GetType()))
			throw new ArgumentException($"Invalid type {o.GetType()} of object {o}");
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
			return false;
		if (!allObjectData.TryGetValue(modUniqueName, out var modObjectData))
			return false;
		return modObjectData.ContainsKey(key);
	}

	public void SetModData<T>(string modUniqueName, object o, string key, T data)
	{
		if (!this.CanHandleType(o.GetType()))
			throw new ArgumentException($"Invalid type {o.GetType()} of object {o}");
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
		{
			allObjectData = new();
			dictionarySetter((TRoot)o, allObjectData);
		}
		
		ref var modObjectData = ref CollectionsMarshal.GetValueRefOrAddDefault(allObjectData, modUniqueName, out var modObjectDataExists);
		if (!modObjectDataExists)
			modObjectData = [];
		modObjectData![key] = data;
	}

	public void RemoveModData(string modUniqueName, object o, string key)
	{
		if (!this.CanHandleType(o.GetType()))
			throw new ArgumentException($"Invalid type {o.GetType()} of object {o}");
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
			return;
		
		if (allObjectData.TryGetValue(modUniqueName, out var modObjectData))
		{
			modObjectData.Remove(key);
			if (modObjectData.Count == 0)
				allObjectData.Remove(modUniqueName);
		}
		if (allObjectData.Count == 0)
			dictionarySetter((TRoot)o, null);
	}

	public bool TryCopyOwnedModDataDirectly(string modUniqueName, object from, object to)
	{
		if (!this.CanHandleType(from.GetType()))
			return false;
		if (!this.CanHandleType(to.GetType()))
			return false;
		if (dictionaryGetter((TRoot)from) is not { } allSourceObjectData)
			return true;
		if (!allSourceObjectData.TryGetValue(modUniqueName, out var sourceModObjectData))
			return true;

		if (dictionaryGetter((TRoot)to) is not { } allTargetObjectData)
		{
			allTargetObjectData = [];
			dictionarySetter((TRoot)to, allTargetObjectData);
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
		if (!this.CanHandleType(from.GetType()))
			return false;
		if (!this.CanHandleType(to.GetType()))
			return false;
		if (dictionaryGetter((TRoot)from) is not { } allSourceObjectData)
			return true;
		
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
		return true;
	}

	public bool TryRemoveOwnedModDataDirectly(string modUniqueName, object o)
	{
		if (!this.CanHandleType(o.GetType()))
			return false;
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
			return true;
		
		if (!allObjectData.Remove(modUniqueName))
			return true;
		if (allObjectData.Count == 0)
			dictionarySetter((TRoot)o, null);
		return true;
	}

	public bool TryRemoveAllModDataDirectly(object o)
	{
		if (!this.CanHandleType(o.GetType()))
			return false;
		if (dictionaryGetter((TRoot)o) is null)
			return true;
		
		dictionarySetter((TRoot)o, null);
		return true;
	}

	public IEnumerable<KeyValuePair<string, object?>> GetAllOwnedModData(string modUniqueName, object o)
	{
		if (!this.CanHandleType(o.GetType()))
			throw new ArgumentException($"Invalid type {o.GetType()} of object {o}");
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
			yield break;
		if (!allObjectData.TryGetValue(modUniqueName, out var modObjectData))
			yield break;
		foreach (var kvp in modObjectData)
			yield return kvp;
	}

	public IEnumerable<KeyValuePair<string, IEnumerable<KeyValuePair<string, object?>>>> GetAllModData(object o)
	{
		if (!this.CanHandleType(o.GetType()))
			throw new ArgumentException($"Invalid type {o.GetType()} of object {o}");
		if (dictionaryGetter((TRoot)o) is not { } allObjectData)
			yield break;
		foreach (var (key, value) in allObjectData)
			yield return new KeyValuePair<string, IEnumerable<KeyValuePair<string, object?>>>(key, value);
	}
}
