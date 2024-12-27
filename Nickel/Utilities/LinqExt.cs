using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

/// <summary>
/// Hosts LINQ-based extensions.
/// </summary>
public static class LinqExt
{
	/// <summary>
	/// Returns the first element or <c>null</c> if there are none.
	/// </summary>
	/// <typeparam name="T">The type of elements.</typeparam>
	/// <param name="self">The enumerable.</param>
	/// <returns>The first element or <c>null</c> if there are none.</returns>
	public static T? FirstOrNull<T>(this IEnumerable<T> self) where T : struct
	{
		if (self.TryGetNonEnumeratedCount(out var count) && count > 0)
			return self.First();
		foreach (var element in self)
			return element;
		return null;
	}

	/// <summary>
	/// Returns the first element matching a predicate or <c>null</c> if there are none.
	/// </summary>
	/// <typeparam name="T">The type of elements.</typeparam>
	/// <param name="self">The enumerable.</param>
	/// <param name="predicate">The predicate.</param>
	/// <returns>The first element matching a predicate or <c>null</c> if there are none.</returns>
	public static T? FirstOrNull<T>(this IEnumerable<T> self, Func<T, bool> predicate) where T : struct
	{
		foreach (var element in self)
			if (predicate(element))
				return element;
		return null;
	}

	/// <summary>
	/// Returns the last element or <c>null</c> if there are none.
	/// </summary>
	/// <typeparam name="T">The type of elements.</typeparam>
	/// <param name="self">The enumerable.</param>
	/// <returns>The last element or <c>null</c> if there are none.</returns>
	public static T? LastOrNull<T>(this IEnumerable<T> self) where T : struct
	{
		if (self.TryGetNonEnumeratedCount(out var count) && count > 0)
			return self.Last();
		foreach (var element in self)
			return element;
		return null;
	}

	/// <summary>
	/// Returns the last element matching a predicate or <c>null</c> if there are none.
	/// </summary>
	/// <typeparam name="T">The type of elements.</typeparam>
	/// <param name="self">The enumerable.</param>
	/// <param name="predicate">The predicate.</param>
	/// <returns>The last element matching a predicate or <c>null</c> if there are none.</returns>
	public static T? LastOrNull<T>(this IEnumerable<T> self, Func<T, bool> predicate) where T : struct
	{
		foreach (var element in self.Reverse())
			if (predicate(element))
				return element;
		return null;
	}
}
