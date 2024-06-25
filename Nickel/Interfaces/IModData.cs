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

public static class IModDataClassExt
{
	public static T? GetOptionalModData<T>(this IModData modData, object o, string key) where T : class
		=> modData.TryGetModData<T>(o, key, out var data) ? data : null;

	public static void SetOptionalModData<T>(this IModData modData, object o, string key, T? data) where T : class
	{
		if (data != null)
			modData.SetModData(o, key, data);
		else
			modData.RemoveModData(o, key);
	}
}

public static class IModDataStructExt
{
	public static T? GetOptionalModData<T>(this IModData modData, object o, string key) where T : struct
		=> modData.TryGetModData<T>(o, key, out var data) ? data : null;

	public static void SetOptionalModData<T>(this IModData modData, object o, string key, T? data) where T : struct
	{
		if (data is { } nonNull)
			modData.SetModData(o, key, nonNull);
		else
			modData.RemoveModData(o, key);
	}
}
