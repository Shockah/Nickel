using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal interface IModDataHandler
{
	bool CanHandleType(Type type);

	IModDataHandler GetUnderlyingHandler(object o);

	T GetModData<T>(string modUniqueName, object o, string key);
	
	bool TryGetModData<T>(string modUniqueName, object o, string key, [MaybeNullWhen(false)] out T data);

	bool ContainsModData(string modUniqueName, object o, string key);

	void SetModData<T>(string modUniqueName, object o, string key, T data);

	void RemoveModData(string modUniqueName, object o, string key);

	bool TryCopyOwnedModDataDirectly(string modUniqueName, object from, object to);

	bool TryCopyAllModDataDirectly(object from, object to);

	IEnumerable<KeyValuePair<string, object?>> GetAllOwnedModData(string modUniqueName, object o);

	IEnumerable<KeyValuePair<string, IEnumerable<KeyValuePair<string, object?>>>> GetAllModData(object o);
	
	T ObtainModData<T>(string modUniqueName, object o, string key, Func<T> factory)
	{
		if (!this.TryGetModData<T>(modUniqueName, o, key, out var data))
		{
			data = factory();
			this.SetModData(modUniqueName, o, key, data);
		}
		return data;
	}
	
	T ObtainModData<T>(string modUniqueName, object o, string key) where T : new()
		=> this.ObtainModData(modUniqueName, o, key, () => new T());
	
	T GetModDataOrDefault<T>(string modUniqueName, object o, string key, T defaultValue)
		=> this.TryGetModData<T>(modUniqueName, o, key, out var value) ? value : defaultValue;
	
	T GetModDataOrDefault<T>(string modUniqueName, object o, string key) where T : new()
		=> this.TryGetModData<T>(modUniqueName, o, key, out var value) ? value : new();
}
