using System;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class ModData(IModManifest modManifest, IModDataHandler modDataHandler) : IModData
{
	public T GetModData<T>(object o, string key)
		=> modDataHandler.GetModData<T>(modManifest, o, key);

	public bool TryGetModData<T>(object o, string key, [MaybeNullWhen(false)] out T data)
		=> modDataHandler.TryGetModData(modManifest, o, key, out data);

	public T GetModDataOrDefault<T>(object o, string key, T defaultValue)
		=> modDataHandler.GetModDataOrDefault(modManifest, o, key, defaultValue);

	public T GetModDataOrDefault<T>(object o, string key) where T : new()
		=> modDataHandler.GetModDataOrDefault<T>(modManifest, o, key);

	public T ObtainModData<T>(object o, string key, Func<T> factory)
		=> modDataHandler.ObtainModData(modManifest, o, key, factory);

	public T ObtainModData<T>(object o, string key) where T : new()
		=> modDataHandler.ObtainModData<T>(modManifest, o, key);

	public bool ContainsModData(object o, string key)
		=> modDataHandler.ContainsModData(modManifest, o, key);

	public void SetModData<T>(object o, string key, T data)
		=> modDataHandler.SetModData(modManifest, o, key, data);

	public void RemoveModData(object o, string key)
		=> modDataHandler.RemoveModData(modManifest, o, key);

	public void CopyOwnedModData(object from, object to)
		=> modDataHandler.CopyOwnedModData(modManifest, from, to);

	public void CopyAllModData(object from, object to)
		=> modDataHandler.CopyAllModData(from, to);
}
