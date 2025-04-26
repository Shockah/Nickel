using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class CompoundModDataHandler<TRoot>(IEnumerable<IModDataHandler<TRoot>> handlers) : IModDataHandler<TRoot> where TRoot : notnull
{
	private readonly Dictionary<Type, IModDataHandler<TRoot>> HandlerCache = [];
	
	private IModDataHandler<TRoot> GetHandler(TRoot o)
	{
		var type = o.GetType();
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
	
	public T GetModData<T>(IModManifest manifest, TRoot o, string key)
		=> this.GetHandler(o).GetModData<T>(manifest, o, key);

	public bool TryGetModData<T>(IModManifest manifest, TRoot o, string key, [MaybeNullWhen(false)] out T data)
		=> this.GetHandler(o).TryGetModData(manifest, o, key, out data);

	public bool ContainsModData(IModManifest manifest, TRoot o, string key)
		=> this.GetHandler(o).ContainsModData(manifest, o, key);

	public void SetModData<T>(IModManifest manifest, TRoot o, string key, T data)
		=> this.GetHandler(o).SetModData(manifest, o, key, data);

	public void RemoveModData(IModManifest manifest, TRoot o, string key)
		=> this.GetHandler(o).RemoveModData(manifest, o, key);

	public void CopyOwnedModData(IModManifest manifest, TRoot from, TRoot to)
		=> this.GetHandler(from).CopyOwnedModData(manifest, from, to);

	public void CopyAllModData(TRoot from, TRoot to)
		=> this.GetHandler(from).CopyAllModData(from, to);
	
	public T ObtainModData<T>(IModManifest manifest, TRoot o, string key, Func<T> factory)
		=> this.GetHandler(o).ObtainModData(manifest, o, key, factory);
}
