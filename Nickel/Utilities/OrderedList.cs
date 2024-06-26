using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Nickel;

/// <summary>
/// Represents a list of elements that is always being kept ordered by a related comparable value.
/// The list can be accessed by index for reading purposes.
/// </summary>
/// <typeparam name="TElement">The type of elements in the list.</typeparam>
/// <typeparam name="TOrderingValue">The assigned <see cref="IComparable{T}"/> value used for ordering purposes.</typeparam>
public sealed class OrderedList<TElement, TOrderingValue> : IReadOnlyList<TElement> where TOrderingValue : IComparable<TOrderingValue>
{
	/// <summary>
	/// Represents a single entry in a <see cref="OrderedList{TElement,TOrderingValue}"/>.
	/// </summary>
	/// <param name="Element">The element.</param>
	/// <param name="OrderingValue">The assigned <see cref="IComparable{T}"/> ordering value.</param>
	public record struct Entry(
		TElement Element,
		TOrderingValue OrderingValue
	);

	/// <summary>An enumerable for all of the entries in the <see cref="OrderedList{TElement,TOrderingValue}"/>.</summary>
	public IEnumerable<Entry> Entries
		=> this.EntryStorage;

	private readonly List<Entry> EntryStorage = [];

	/// <summary>Whether the <see cref="OrderedList{TElement,TOrderingValue}"/> orders its elements by their assigned <see cref="IComparable{T}"/> ordering values in an ascending order (as opposed to a descending order).</summary>
	public readonly bool Ascending;

	/// <summary>
	/// Initializes a new <see cref="OrderedList{TElement,TOrderingValue}"/> which orders its elements by their assigned <see cref="IComparable{T}"/> ordering values in an ascending order.
	/// </summary>
	public OrderedList() : this(ascending: true) { }

	/// <summary>
	/// Initializes a new <see cref="OrderedList{TElement,TOrderingValue}"/> which orders its elements by their assigned <see cref="IComparable{T}"/> ordering values in the given order.
	/// </summary>
	/// <param name="ascending">Whether the <see cref="OrderedList{TElement,TOrderingValue}"/> orders its elements by their assigned <see cref="IComparable{T}"/> ordering values in an ascending order (as opposed to a descending order).</param>
	public OrderedList(bool ascending)
	{
		this.Ascending = ascending;
	}

	/// <summary>
	/// Makes a copy of the given <see cref="OrderedList{TElement,TOrderingValue}"/>.
	/// </summary>
	/// <param name="anotherList">The list to copy.</param>
	public OrderedList(OrderedList<TElement, TOrderingValue> anotherList)
	{
		this.Ascending = anotherList.Ascending;
		this.EntryStorage.AddRange(anotherList.EntryStorage);
	}

	/// <inheritdoc/>
	public int Count
		=> this.EntryStorage.Count;

	/// <inheritdoc/>
	public TElement this[int index]
		=> this.EntryStorage[index].Element;

	/// <inheritdoc/>
	public IEnumerator<TElement> GetEnumerator()
		=> this.EntryStorage.Select(e => e.Element).GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
		=> this.GetEnumerator();

	/// <summary>
	/// Removes all elements from the <see cref="OrderedList{TElement,TOrderingValue}"/>.
	/// </summary>
	public void Clear()
		=> this.EntryStorage.Clear();

	/// <summary>
	/// Checks whether the <see cref="OrderedList{TElement,TOrderingValue}"/> contains the given element.
	/// </summary>
	/// <param name="item">The element to check.</param>
	/// <returns>Whether the list contains the given element.</returns>
	public bool Contains(TElement item)
		=> this.EntryStorage.Any(e => Equals(e.Element, item));

	/// <summary>
	/// Adds an element to the <see cref="OrderedList{TElement,TOrderingValue}"/>.
	/// </summary>
	/// <param name="element">The element to be added.</param>
	/// <param name="orderingValue">The <see cref="IComparable{T}"/> ordering value that will be used to order the element with in the collection.</param>
	public void Add(TElement element, TOrderingValue orderingValue)
	{
		if (this.Ascending)
		{
			for (var i = 0; i < this.EntryStorage.Count; i++)
			{
				if (this.EntryStorage[i].OrderingValue.CompareTo(orderingValue) > 0)
				{
					this.EntryStorage.Insert(i, new(element, orderingValue));
					return;
				}
			}
		}
		else
		{
			for (var i = 0; i < this.EntryStorage.Count; i++)
			{
				if (this.EntryStorage[i].OrderingValue.CompareTo(orderingValue) < 0)
				{
					this.EntryStorage.Insert(i, new(element, orderingValue));
					return;
				}
			}
		}

		this.EntryStorage.Add(new(element, orderingValue));
	}

	/// <summary>
	/// Removes an element from the <see cref="OrderedList{TElement,TOrderingValue}"/>.
	/// </summary>
	/// <param name="element">The element to be removed.</param>
	/// <returns>Whether the collection actually contained the element and it was removed.</returns>
	public bool Remove(TElement element)
	{
		for (var i = 0; i < this.EntryStorage.Count; i++)
		{
			if (Equals(this.EntryStorage[i].Element, element))
			{
				this.EntryStorage.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Attempt to get the <see cref="IComparable{T}"/> ordering value assigned to the given element.
	/// </summary>
	/// <param name="element">The element to get the ordering value for.</param>
	/// <param name="orderingValue">The retrieved ordering value, if succeeded.</param>
	/// <returns>Whether the collection actually contained the element and retrieving the ordering value succeeded.</returns>
	public bool TryGetOrderingValue(TElement element, [MaybeNullWhen(false)] out TOrderingValue orderingValue)
	{
		foreach (var entry in this.EntryStorage)
		{
			if (!Equals(entry.Element, element))
				continue;
			orderingValue = entry.OrderingValue;
			return true;
		}
		orderingValue = default;
		return false;
	}
}
