using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Nickel;

internal sealed class ModData(
	IModManifest modManifest,
	IModDataHandler modDataHandler
) : IModData
{
	public T GetModData<T>(object o, string key)
		=> modDataHandler.GetModData<T>(modManifest.UniqueName, o, key);

	public bool TryGetModData<T>(object o, string key, [MaybeNullWhen(false)] out T data)
		=> modDataHandler.TryGetModData(modManifest.UniqueName, o, key, out data);

	public T GetModDataOrDefault<T>(object o, string key, T defaultValue)
		=> modDataHandler.GetModDataOrDefault(modManifest.UniqueName, o, key, defaultValue);

	public T GetModDataOrDefault<T>(object o, string key) where T : new()
		=> modDataHandler.GetModDataOrDefault<T>(modManifest.UniqueName, o, key);

	public T ObtainModData<T>(object o, string key, Func<T> factory)
		=> modDataHandler.ObtainModData(modManifest.UniqueName, o, key, factory);

	public T ObtainModData<T>(object o, string key) where T : new()
		=> modDataHandler.ObtainModData<T>(modManifest.UniqueName, o, key);

	public bool ContainsModData(object o, string key)
		=> modDataHandler.ContainsModData(modManifest.UniqueName, o, key);

	public void SetModData<T>(object o, string key, T data)
		=> modDataHandler.SetModData(modManifest.UniqueName, o, key, data);

	public void RemoveModData(object o, string key)
		=> modDataHandler.RemoveModData(modManifest.UniqueName, o, key);

	public void CopyOwnedModData(object from, object to)
	{
		if (modDataHandler.GetUnderlyingHandler(from) == modDataHandler.GetUnderlyingHandler(to) && modDataHandler.TryCopyOwnedModDataDirectly(modManifest.UniqueName, from, to))
			return;
		foreach (var (key, value) in modDataHandler.GetAllOwnedModData(modManifest.UniqueName, from))
			modDataHandler.SetModData(modManifest.UniqueName, to, key, value);
	}

	public void CopyAllModData(object from, object to)
	{
		if (modDataHandler.GetUnderlyingHandler(from) == modDataHandler.GetUnderlyingHandler(to) && modDataHandler.TryCopyAllModDataDirectly(from, to))
			return;
		foreach (var (modUniqueName, data) in modDataHandler.GetAllModData(from))
			foreach (var (key, value) in data)
				modDataHandler.SetModData(modUniqueName, to, key, value);
	}

	public void RemoveOwnedModData(object o)
	{
		if (modDataHandler.TryRemoveOwnedModDataDirectly(modManifest.UniqueName, o))
			return;
		foreach (var key in modDataHandler.GetAllOwnedModData(modManifest.UniqueName, o).Select(kvp => kvp.Key).ToList())
			modDataHandler.RemoveModData(modManifest.UniqueName, o, key);
	}

	public void RemoveAllModData(object o)
	{
		if (modDataHandler.TryRemoveAllModDataDirectly(o))
			return;
		foreach (var (modUniqueName, keys) in modDataHandler.GetAllModData(o).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(kvp2 => kvp2.Key).ToList()))
			foreach (var key in keys)
				modDataHandler.RemoveModData(modUniqueName, o, key);
	}
}

internal sealed class VanillaModData(
	IModDataHandler modDataHandler
) : IModData
{
	public T GetModData<T>(object o, string key)
		=> throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");

	public bool TryGetModData<T>(object o, string key, [MaybeNullWhen(false)] out T data)
	{
		data = default;
		return false;
	}

	public T GetModDataOrDefault<T>(object o, string key, T defaultValue)
		=> defaultValue;

	public T GetModDataOrDefault<T>(object o, string key) where T : new()
		=> new();

	public T ObtainModData<T>(object o, string key, Func<T> factory)
		=> throw new NotSupportedException();

	public T ObtainModData<T>(object o, string key) where T : new()
		=> throw new NotSupportedException();

	public bool ContainsModData(object o, string key)
		=> false;

	public void SetModData<T>(object o, string key, T data)
		=> throw new NotSupportedException();

	public void RemoveModData(object o, string key) { }

	public void CopyOwnedModData(object from, object to) { }

	public void CopyAllModData(object from, object to)
	{
		if (modDataHandler.GetUnderlyingHandler(from) == modDataHandler.GetUnderlyingHandler(to) && modDataHandler.TryCopyAllModDataDirectly(from, to))
			return;
		foreach (var (modUniqueName, data) in modDataHandler.GetAllModData(from))
			foreach (var (key, value) in data)
				modDataHandler.SetModData(modUniqueName, to, key, value);
	}

	public void RemoveOwnedModData(object o) { }

	public void RemoveAllModData(object o)
	{
		if (modDataHandler.TryRemoveAllModDataDirectly(o))
			return;
		foreach (var (modUniqueName, keys) in modDataHandler.GetAllModData(o).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(kvp2 => kvp2.Key).ToList()))
			foreach (var key in keys)
				modDataHandler.RemoveModData(modUniqueName, o, key);
	}
}
