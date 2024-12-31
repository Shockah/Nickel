using System;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Represents a pool of objects of a single type.
/// </summary>
/// <param name="factory">A function that creates new objects of the given type.</param>
/// <typeparam name="T">The type of objects stored and created by this pool.</typeparam>
public sealed class Pool<T>(
	Func<T> factory,
	Action<T>? onReturn = null,
	Action<T>? onReuse = null
) where T : class
{
	private readonly Queue<T> Queue = [];

	/// <summary>
	/// Retrieves a free or a new object of this pool's type.
	/// </summary>
	/// <returns>The free or new object.</returns>
	public T Get()
	{
		if (this.Queue.Count == 0)
			return factory();

		var @object = this.Queue.Dequeue();
		onReuse?.Invoke(@object);
		return @object;
	}

	/// <summary>
	/// Marks the given object as being able to be reused by successive calls to <see cref="Get"/>.
	/// </summary>
	/// <param name="object">The object to reuse.</param>
	public void Return(T @object)
	{
		onReturn?.Invoke(@object);
		this.Queue.Enqueue(@object);
	}

	/// <summary>
	/// Runs the given action on a newly retrieved object, which is then safely returned to the pool.
	/// </summary>
	/// <param name="action">The action to run.</param>
	public void Do(Action<T> action)
	{
		var @object = this.Get();
		try
		{
			action(@object);
		}
		finally
		{
			this.Return(@object);
		}
	}

	/// <summary>
	/// Runs the given action on a newly retrieved object, which is then safely returned to the pool.
	/// </summary>
	/// <param name="func">The action to run.</param>
	/// <typeparam name="R">The type of object to return.</typeparam>
	public R Do<R>(Func<T, R> func)
	{
		var @object = this.Get();
		try
		{
			return func(@object);
		}
		finally
		{
			this.Return(@object);
		}
	}
}
