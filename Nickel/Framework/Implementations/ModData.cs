using System;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class ModData(IModManifest modManifest, ModDataManager modDataManager) : IModData
{
	public T GetModData<T>(object o, string key)
		=> modDataManager.GetModData<T>(modManifest, o, key);

	public bool TryGetModData<T>(object o, string key, [MaybeNullWhen(false)] out T data)
		=> modDataManager.TryGetModData(modManifest, o, key, out data);

	public T GetModDataOrDefault<T>(object o, string key, T defaultValue)
		=> modDataManager.GetModDataOrDefault(modManifest, o, key, defaultValue);

	public T GetModDataOrDefault<T>(object o, string key) where T : new()
		=> modDataManager.GetModDataOrDefault<T>(modManifest, o, key);

	public T ObtainModData<T>(object o, string key, Func<T> factory)
		=> modDataManager.ObtainModData(modManifest, o, key, factory);

	public T ObtainModData<T>(object o, string key) where T : new()
		=> modDataManager.ObtainModData<T>(modManifest, o, key);

	public bool ContainsModData(object o, string key)
		=> modDataManager.ContainsModData(modManifest, o, key);

	public void SetModData<T>(object o, string key, T data)
		=> modDataManager.SetModData(modManifest, o, key, data);

	public void RemoveModData(object o, string key)
		=> modDataManager.RemoveModData(modManifest, o, key);

	public void CopyOwnedModData(object from, object to)
		=> modDataManager.CopyOwnedModData(modManifest, from, to);

	public void CopyAllModData(object from, object to)
		=> modDataManager.CopyAllModData(from, to);
}
