using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class CompoundModDataHandler(IEnumerable<IModDataHandler> handlers) : IModDataHandler
{
	private readonly Dictionary<Type, IModDataHandler> HandlerCache = [];

	private IModDataHandler GetHandler(object o)
		=> this.GetHandler(o.GetType());
	
	private IModDataHandler GetHandler(Type type)
	{
		if (this.HandlerCache.TryGetValue(type, out var handler))
			return handler;

		foreach (var enumerableHandler in handlers)
		{
			if (!enumerableHandler.CanHandleType(type))
				continue;

			this.HandlerCache[type] = enumerableHandler;
			return enumerableHandler;
		}

		throw new InvalidOperationException($"Cannot handle mod data for type {type}");
	}

	public bool CanHandleType(Type type)
	{
		try
		{
			return this.GetHandler(type).CanHandleType(type);
		}
		catch
		{
			return false;
		}
	}

	public IModDataHandler GetUnderlyingHandler(object o)
		=> this.GetHandler(o);

	public T GetModData<T>(string modUniqueName, object o, string key)
		=> this.GetHandler(o).GetModData<T>(modUniqueName, o, key);

	public bool TryGetModData<T>(string modUniqueName, object o, string key, [MaybeNullWhen(false)] out T data)
		=> this.GetHandler(o).TryGetModData(modUniqueName, o, key, out data);

	public bool ContainsModData(string modUniqueName, object o, string key)
		=> this.GetHandler(o).ContainsModData(modUniqueName, o, key);

	public void SetModData<T>(string modUniqueName, object o, string key, T data)
		=> this.GetHandler(o).SetModData(modUniqueName, o, key, data);

	public void RemoveModData(string modUniqueName, object o, string key)
		=> this.GetHandler(o).RemoveModData(modUniqueName, o, key);

	public bool TryCopyOwnedModDataDirectly(string modUniqueName, object from, object to)
		=> this.GetHandler(from).TryCopyOwnedModDataDirectly(modUniqueName, from, to);

	public bool TryCopyAllModDataDirectly(object from, object to)
		=> this.GetHandler(from).TryCopyAllModDataDirectly(from, to);

	public IEnumerable<KeyValuePair<string, object?>> GetAllOwnedModData(string modUniqueName, object o)
		=> this.GetHandler(o).GetAllOwnedModData(modUniqueName, o);

	public IEnumerable<KeyValuePair<string, IEnumerable<KeyValuePair<string, object?>>>> GetAllModData(object o)
		=> this.GetHandler(o).GetAllModData(o);

	public T ObtainModData<T>(string modUniqueName, object o, string key, Func<T> factory)
		=> this.GetHandler(o).ObtainModData(modUniqueName, o, key, factory);
}
