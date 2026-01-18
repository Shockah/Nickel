using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

/// <summary>
/// An <see cref="IReadOnlySet{T}"/> implementation built on top of a list.
/// </summary>
/// <param name="list">The list to build this set on top of.</param>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public sealed class ReadOnlySetFromList<T>(IReadOnlyList<T> list) : IReadOnlySet<T>
{
	/// <inheritdoc/>
	public IEnumerator<T> GetEnumerator()
		=> list.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
		=> list.GetEnumerator();

	/// <inheritdoc/>
	public int Count
		=> list.Count;

	/// <inheritdoc/>
	public bool Contains(T item)
		=> list.Contains(item);

	/// <inheritdoc/>
	public bool IsProperSubsetOf(IEnumerable<T> other)
	{
		if (other.TryGetNonEnumeratedCount(out var count))
		{
			if (count > list.Count)
				return false;
			return list.All(other.ToList().Contains);
		}

		var otherList = other.ToList();
		if (otherList.Count > list.Count)
			return false;
		return list.All(otherList.Contains);
	}

	/// <inheritdoc/>
	public bool IsProperSupersetOf(IEnumerable<T> other)
	{
		if (other.TryGetNonEnumeratedCount(out var count))
			return count >= list.Count && other.All(list.Contains);

		var otherList = other.ToList();
		return otherList.Count >= list.Count && otherList.All(list.Contains);
	}

	/// <inheritdoc/>
	public bool IsSubsetOf(IEnumerable<T> other)
	{
		var otherList = other.ToList();
		return list.All(otherList.Contains);
	}

	/// <inheritdoc/>
	public bool IsSupersetOf(IEnumerable<T> other)
		=> other.All(list.Contains);

	/// <inheritdoc/>
	public bool Overlaps(IEnumerable<T> other)
		=> other.Any(list.Contains);

	/// <inheritdoc/>
	public bool SetEquals(IEnumerable<T> other)
	{
		if (other.TryGetNonEnumeratedCount(out var count))
			return count == list.Count && other.All(list.Contains);

		var otherList = other.ToList();
		return otherList.Count == list.Count && otherList.All(list.Contains);
	}
}
