using System;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal interface IModDataHandler<in TRoot> where TRoot : notnull
{
	bool CanHandleType(Type type)
		=> type.IsAssignableTo(typeof(TRoot));
	
	T GetModData<T>(IModManifest manifest, TRoot o, string key);
	
	bool TryGetModData<T>(IModManifest manifest, TRoot o, string key, [MaybeNullWhen(false)] out T data);

	bool ContainsModData(IModManifest manifest, TRoot o, string key);

	void SetModData<T>(IModManifest manifest, TRoot o, string key, T data);

	void RemoveModData(IModManifest manifest, TRoot o, string key);

	void CopyOwnedModData(IModManifest manifest, TRoot from, TRoot to);

	void CopyAllModData(TRoot from, TRoot to);
	
	T ObtainModData<T>(IModManifest manifest, TRoot o, string key, Func<T> factory)
	{
		if (!this.TryGetModData<T>(manifest, o, key, out var data))
		{
			data = factory();
			this.SetModData(manifest, o, key, data);
		}
		return data;
	}
	
	T ObtainModData<T>(IModManifest manifest, TRoot o, string key) where T : new()
		=> this.ObtainModData(manifest, o, key, () => new T());
	
	T GetModDataOrDefault<T>(IModManifest manifest, TRoot o, string key, T defaultValue)
		=> this.TryGetModData<T>(manifest, o, key, out var value) ? value : defaultValue;
	
	T GetModDataOrDefault<T>(IModManifest manifest, TRoot o, string key) where T : new()
		=> this.TryGetModData<T>(manifest, o, key, out var value) ? value : new();
}
