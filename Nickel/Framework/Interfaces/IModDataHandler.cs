using System;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal interface IModDataHandler
{
	bool CanHandleType(Type type);
	
	T GetModData<T>(IModManifest manifest, object o, string key);
	
	bool TryGetModData<T>(IModManifest manifest, object o, string key, [MaybeNullWhen(false)] out T data);

	bool ContainsModData(IModManifest manifest, object o, string key);

	void SetModData<T>(IModManifest manifest, object o, string key, T data);

	void RemoveModData(IModManifest manifest, object o, string key);

	void CopyOwnedModData(IModManifest manifest, object from, object to);

	void CopyAllModData(object from, object to);
	
	T ObtainModData<T>(IModManifest manifest, object o, string key, Func<T> factory)
	{
		if (!this.TryGetModData<T>(manifest, o, key, out var data))
		{
			data = factory();
			this.SetModData(manifest, o, key, data);
		}
		return data;
	}
	
	T ObtainModData<T>(IModManifest manifest, object o, string key) where T : new()
		=> this.ObtainModData(manifest, o, key, () => new T());
	
	T GetModDataOrDefault<T>(IModManifest manifest, object o, string key, T defaultValue)
		=> this.TryGetModData<T>(manifest, o, key, out var value) ? value : defaultValue;
	
	T GetModDataOrDefault<T>(IModManifest manifest, object o, string key) where T : new()
		=> this.TryGetModData<T>(manifest, o, key, out var value) ? value : new();
}
