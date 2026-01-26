using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

/// <summary>
/// Hosts LINQ-based extensions.
/// </summary>
public static class LinqExt
{
	/// <param name="enumerable">The enumerable.</param>
	/// <typeparam name="T">The type of elements.</typeparam>
	extension<T>(IEnumerable<T> enumerable) where T : struct
	{
		/// <summary>
		/// Returns the first element or <c>null</c> if there are none.
		/// </summary>
		/// <returns>The first element or <c>null</c> if there are none.</returns>
		public T? FirstOrNull()
		{
			if (enumerable.TryGetNonEnumeratedCount(out var count) && count > 0)
				return enumerable.First();
			foreach (var element in enumerable)
				return element;
			return null;
		}

		/// <summary>
		/// Returns the first element matching a predicate or <c>null</c> if there are none.
		/// </summary>
		/// <param name="predicate">The predicate.</param>
		/// <returns>The first element matching a predicate or <c>null</c> if there are none.</returns>
		public T? FirstOrNull(Func<T, bool> predicate)
		{
			foreach (var element in enumerable)
				if (predicate(element))
					return element;
			return null;
		}

		/// <summary>
		/// Returns the last element or <c>null</c> if there are none.
		/// </summary>
		/// <returns>The last element or <c>null</c> if there are none.</returns>
		public T? LastOrNull()
		{
			if (enumerable.TryGetNonEnumeratedCount(out var count) && count > 0)
				return enumerable.Last();
			foreach (var element in enumerable)
				return element;
			return null;
		}

		/// <summary>
		/// Returns the last element matching a predicate or <c>null</c> if there are none.
		/// </summary>
		/// <param name="predicate">The predicate.</param>
		/// <returns>The last element matching a predicate or <c>null</c> if there are none.</returns>
		public T? LastOrNull(Func<T, bool> predicate)
		{
			foreach (var element in enumerable.Reverse())
				if (predicate(element))
					return element;
			return null;
		}
	}
}
