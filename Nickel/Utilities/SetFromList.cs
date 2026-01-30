using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

/// <summary>
/// An <see cref="ISet{T}"/> implementation built on top of a list.
/// </summary>
/// <param name="list">The list to build this set on top of.</param>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public sealed class SetFromList<T>(List<T> list) : ISet<T>, IReadOnlySet<T>
{
	/// <inheritdoc/>
	public IEnumerator<T> GetEnumerator()
		=> list.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
		=> list.GetEnumerator();

	/// <inheritdoc/>
	public bool Remove(T item)
		=> list.Remove(item);

	/// <inheritdoc cref="P:ISet{T}.Count"/>
	public int Count
		=> list.Count;

	/// <inheritdoc/>
	public bool IsReadOnly
		=> false;

	/// <inheritdoc/>
	void ICollection<T>.Add(T item)
		=> this.Add(item);

	/// <inheritdoc/>
	public void ExceptWith(IEnumerable<T> other)
	{
		foreach (var element in other)
			list.Remove(element);
	}

	/// <inheritdoc/>
	public void IntersectWith(IEnumerable<T> other)
	{
		var otherList = other.ToList();
		for (var i = list.Count - 1; i >= 0; i--)
			if (otherList.Contains(list[i]))
				list.RemoveAt(i);
	}

	/// <inheritdoc/>
	public bool Add(T item)
	{
		if (list.Contains(item))
			return false;
		list.Add(item);
		return true;
	}

	/// <inheritdoc/>
	public void Clear()
		=> list.Clear();

	/// <inheritdoc cref="M:ISet{T}.Contains"/>
	public bool Contains(T item)
		=> list.Contains(item);

	/// <inheritdoc/>
	public void CopyTo(T[] array, int arrayIndex)
		=> list.CopyTo(array, arrayIndex);

	/// <inheritdoc cref="ISet{T}.IsProperSubsetOf"/>
	public bool IsProperSubsetOf(IEnumerable<T> other)
	{
		if (other.TryGetNonEnumeratedCount(out var count))
			return count <= list.Count && list.All(other.ToList().Contains);

		var otherList = other.ToList();
		return otherList.Count <= list.Count && list.All(otherList.Contains);
	}

	/// <inheritdoc cref="ISet{T}.IsProperSupersetOf"/>
	public bool IsProperSupersetOf(IEnumerable<T> other)
	{
		if (other.TryGetNonEnumeratedCount(out var count))
			return count >= list.Count && other.All(list.Contains);

		var otherList = other.ToList();
		return otherList.Count >= list.Count && otherList.All(list.Contains);
	}

	/// <inheritdoc cref="ISet{T}.IsSubsetOf"/>
	public bool IsSubsetOf(IEnumerable<T> other)
	{
		var otherList = other.ToList();
		return list.All(otherList.Contains);
	}

	/// <inheritdoc cref="ISet{T}.IsSupersetOf"/>
	public bool IsSupersetOf(IEnumerable<T> other)
		=> other.All(list.Contains);

	/// <inheritdoc cref="ISet{T}.Overlaps"/>
	public bool Overlaps(IEnumerable<T> other)
		=> other.Any(list.Contains);

	/// <inheritdoc cref="ISet{T}.SetEquals"/>
	public bool SetEquals(IEnumerable<T> other)
	{
		if (other.TryGetNonEnumeratedCount(out var count))
			return count == list.Count && other.All(list.Contains);

		var otherList = other.ToList();
		return otherList.Count == list.Count && otherList.All(list.Contains);
	}

	/// <inheritdoc/>
	public void SymmetricExceptWith(IEnumerable<T> other)
	{
		foreach (var element in other)
		{
			if (this.Contains(element))
				this.Remove(element);
			else
				this.Add(element);
		}
	}

	/// <inheritdoc/>
	public void UnionWith(IEnumerable<T> other)
	{
		foreach (var element in other)
			if (!this.Contains(element))
				this.Add(element);
	}
}
