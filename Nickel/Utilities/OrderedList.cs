using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nickel;

/// <summary>
/// Represents a list of elements that is always being kept ordered by a related comparable value.
/// The list can be accessed by index for reading purposes.
/// </summary>
/// <typeparam name="TElement">The type of elements in the list.</typeparam>
/// <typeparam name="TOrderingValue">The assigned <see cref="IComparable{T}"/> value used for ordering purposes.</typeparam>
public sealed class OrderedList<TElement, TOrderingValue> : IReadOnlyList<TElement>
	where TElement : notnull
	where TOrderingValue : IComparable<TOrderingValue>
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

	private readonly List<Entry> EntryStorage;
	private readonly Dictionary<TElement, List<TOrderingValue>> ElementToOrderingValues;

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
		this.EntryStorage = [];
		this.ElementToOrderingValues = [];
	}

	/// <summary>
	/// Makes a copy of the given <see cref="OrderedList{TElement,TOrderingValue}"/>.
	/// </summary>
	/// <param name="anotherList">The list to copy.</param>
	public OrderedList(OrderedList<TElement, TOrderingValue> anotherList)
	{
		this.Ascending = anotherList.Ascending;
		this.EntryStorage = anotherList.EntryStorage.ToList();
		this.ElementToOrderingValues = anotherList.ElementToOrderingValues.ToDictionary(
			kvp => kvp.Key,
			kvp => kvp.Value.ToList()
		);
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
	{
		this.EntryStorage.Clear();
		this.ElementToOrderingValues.Clear();
	}

	/// <summary>
	/// Checks whether the <see cref="OrderedList{TElement,TOrderingValue}"/> contains the given element.
	/// </summary>
	/// <param name="item">The element to check.</param>
	/// <returns>Whether the list contains the given element.</returns>
	public bool Contains(TElement item)
		=> this.ElementToOrderingValues.ContainsKey(item);

	/// <summary>
	/// Adds an element to the <see cref="OrderedList{TElement,TOrderingValue}"/>.
	/// </summary>
	/// <param name="element">The element to be added.</param>
	/// <param name="orderingValue">The <see cref="IComparable{T}"/> ordering value that will be used to order the element with in the collection.</param>
	public void Add(TElement element, TOrderingValue orderingValue)
	{
		var entry = new Entry(element, orderingValue);
		
		ref var orderingValues = ref CollectionsMarshal.GetValueRefOrAddDefault(this.ElementToOrderingValues, element, out var orderingValuesExist);
		if (!orderingValuesExist)
			orderingValues = [];
		
		HandleOrderingValues(ref orderingValues!);
		HandleEntries();
		
		void HandleOrderingValues(ref List<TOrderingValue> orderingValues)
		{
			if (orderingValues.Count == 0)
			{
				orderingValues.Add(orderingValue);
				return;
			}
			
			if (this.Ascending)
			{
				if (orderingValues[^1].CompareTo(orderingValue) <= 0)
				{
					orderingValues.Add(orderingValue);
					return;
				}
				
				for (var i = orderingValues.Count - 2; i >= 0; i--)
				{
					if (orderingValues[i].CompareTo(orderingValue) > 0)
						continue;
					orderingValues.Insert(i, orderingValue);
					return;
				}
			}
			else
			{
				if (orderingValues[^1].CompareTo(orderingValue) >= 0)
				{
					orderingValues.Add(orderingValue);
					return;
				}
				
				for (var i = orderingValues.Count - 2; i >= 0; i--)
				{
					if (orderingValues[i].CompareTo(orderingValue) < 0)
						continue;
					orderingValues.Insert(i, orderingValue);
					return;
				}
			}

			orderingValues.Insert(0, orderingValue);
		}

		void HandleEntries()
		{
			if (this.EntryStorage.Count == 0)
			{
				this.EntryStorage.Add(entry);
				return;
			}
			
			if (this.Ascending)
			{
				if (this.EntryStorage[^1].OrderingValue.CompareTo(orderingValue) <= 0)
				{
					this.EntryStorage.Add(entry);
					return;
				}
				
				for (var i = this.EntryStorage.Count - 2; i >= 0; i--)
				{
					if (this.EntryStorage[i].OrderingValue.CompareTo(orderingValue) > 0)
						continue;
					this.EntryStorage.Insert(i, entry);
				}
			}
			else
			{
				if (this.EntryStorage[^1].OrderingValue.CompareTo(orderingValue) >= 0)
				{
					this.EntryStorage.Add(entry);
					return;
				}
				
				for (var i = this.EntryStorage.Count - 2; i >= 0; i--)
				{
					if (this.EntryStorage[i].OrderingValue.CompareTo(orderingValue) < 0)
						continue;
					this.EntryStorage.Insert(i, entry);
					return;
				}
			}

			this.EntryStorage.Insert(0, entry);
		}
	}

	/// <summary>
	/// Removes an element from the <see cref="OrderedList{TElement,TOrderingValue}"/>.
	/// </summary>
	/// <param name="element">The element to be removed.</param>
	/// <returns>Whether the collection actually contained the element and it was removed.</returns>
	public bool Remove(TElement element)
	{
		if (!this.ElementToOrderingValues.TryGetValue(element, out var orderingValues))
			return false;
		
		orderingValues.RemoveAt(orderingValues.Count - 1);
		if (orderingValues.Count == 0)
			this.ElementToOrderingValues.Remove(element);

		for (var i = this.ElementToOrderingValues.Count - 1; i >= 0; i--)
		{
			if (!Equals(this.EntryStorage[i].Element, element))
				continue;
			this.EntryStorage.RemoveAt(i);
			return true;
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
		if (!this.ElementToOrderingValues.TryGetValue(element, out var orderingValues))
		{
			orderingValue = default;
			return false;
		}
		
		orderingValue = orderingValues[0];
		return true;
	}
}
