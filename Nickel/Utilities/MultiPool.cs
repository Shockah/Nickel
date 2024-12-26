using System;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Represents a pool of objects of multiple types.
/// </summary>
public sealed class MultiPool
{
	private readonly Dictionary<Type, object> Pools = [];

	/// <summary>
	/// Retrieves a free or a new object of the given type.
	/// </summary>
	/// <typeparam name="T">The type of objects.</typeparam>
	/// <returns>The free or new object.</returns>
	public T Get<T>() where T : class, new()
		=> this.GetPool<T>().Get();

	/// <summary>
	/// Marks the given object as being able to be reused by successive calls to <see cref="Get{T}"/>.
	/// </summary>
	/// <param name="object">The object to reuse.</param>
	/// <typeparam name="T">The type of object.</typeparam>
	public void Return<T>(T @object) where T : class, new()
		=> this.GetPool<T>().Return(@object);

	/// <summary>
	/// Runs the given action on a newly retrieved object, which is then safely returned to the pool.
	/// </summary>
	/// <param name="action">The action to run.</param>
	/// <typeparam name="T">The type of object.</typeparam>
	public void Do<T>(Action<T> action) where T : class, new()
		=> this.GetPool<T>().Do(action);

	/// <summary>
	/// Runs the given action on a newly retrieved object, which is then safely returned to the pool.
	/// </summary>
	/// <param name="func">The action to run.</param>
	/// <typeparam name="T">The type of object.</typeparam>
	/// <typeparam name="R">The type of object to return.</typeparam>
	public R Do<T, R>(Func<T, R> func) where T : class, new()
		=> this.GetPool<T>().Do(func);

	private Pool<T> GetPool<T>() where T : class, new()
	{
		if (!this.Pools.TryGetValue(typeof(T), out var rawPool))
		{
			rawPool = new Pool<T>(() => new T());
			this.Pools[typeof(T)] = rawPool;
		}
		return (Pool<T>)rawPool;
	}
}
