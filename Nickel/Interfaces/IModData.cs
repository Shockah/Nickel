using System;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

public interface IModData
{
	T GetModData<T>(object o, string key);
	bool TryGetModData<T>(object o, string key, [MaybeNullWhen(false)] out T data);
	T GetModDataOrDefault<T>(object o, string key, T defaultValue);
	T GetModDataOrDefault<T>(object o, string key) where T : new();
	T ObtainModData<T>(object o, string key, Func<T> factory);
	T ObtainModData<T>(object o, string key) where T : new();
	bool ContainsModData(object o, string key);
	void SetModData<T>(object o, string key, T data);
	void RemoveModData(object o, string key);
}
