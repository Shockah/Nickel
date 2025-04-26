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
	
	public T GetModData<T>(IModManifest manifest, object o, string key)
		=> this.GetHandler(o).GetModData<T>(manifest, o, key);

	public bool TryGetModData<T>(IModManifest manifest, object o, string key, [MaybeNullWhen(false)] out T data)
		=> this.GetHandler(o).TryGetModData(manifest, o, key, out data);

	public bool ContainsModData(IModManifest manifest, object o, string key)
		=> this.GetHandler(o).ContainsModData(manifest, o, key);

	public void SetModData<T>(IModManifest manifest, object o, string key, T data)
		=> this.GetHandler(o).SetModData(manifest, o, key, data);

	public void RemoveModData(IModManifest manifest, object o, string key)
		=> this.GetHandler(o).RemoveModData(manifest, o, key);

	public void CopyOwnedModData(IModManifest manifest, object from, object to)
		=> this.GetHandler(from).CopyOwnedModData(manifest, from, to);

	public void CopyAllModData(object from, object to)
		=> this.GetHandler(from).CopyAllModData(from, to);
	
	public T ObtainModData<T>(IModManifest manifest, object o, string key, Func<T> factory)
		=> this.GetHandler(o).ObtainModData(manifest, o, key, factory);
}
