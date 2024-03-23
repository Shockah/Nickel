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
		=> self.Select(e => new T?(e)).FirstOrDefault();

	/// <summary>
	/// Returns the first element matching a predicate or <c>null</c> if there are none.
	/// </summary>
	/// <typeparam name="T">The type of elements.</typeparam>
	/// <param name="self">The enumerable.</param>
	/// <param name="predicate">The predicate.</param>
	/// <returns>The first element matching a predicate or <c>null</c> if there are none.</returns>
	public static T? FirstOrNull<T>(this IEnumerable<T> self, Func<T, bool> predicate) where T : struct
		=> self.Where(predicate).Select(e => new T?(e)).FirstOrDefault();

	/// <summary>
	/// Returns the last element or <c>null</c> if there are none.
	/// </summary>
	/// <typeparam name="T">The type of elements.</typeparam>
	/// <param name="self">The enumerable.</param>
	/// <returns>The last element or <c>null</c> if there are none.</returns>
	public static T? LastOrNull<T>(this IEnumerable<T> self) where T : struct
		=> self.Select(e => new T?(e)).LastOrDefault();

	/// <summary>
	/// Returns the last element matching a predicate or <c>null</c> if there are none.
	/// </summary>
	/// <typeparam name="T">The type of elements.</typeparam>
	/// <param name="self">The enumerable.</param>
	/// <param name="predicate">The predicate.</param>
	/// <returns>The last element matching a predicate or <c>null</c> if there are none.</returns>
	public static T? LastOrNull<T>(this IEnumerable<T> self, Func<T, bool> predicate) where T : struct
		=> self.Where(predicate).Select(e => new T?(e)).LastOrDefault();
}
