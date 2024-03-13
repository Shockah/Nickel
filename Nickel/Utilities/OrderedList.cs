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
/// <typeparam name="TOrderingValue">The related comparable value used for ordering purposes.</typeparam>
public sealed class OrderedList<TElement, TOrderingValue> : IReadOnlyList<TElement> where TOrderingValue : IComparable<TOrderingValue>
{
	public record struct Entry(
		TElement Element,
		TOrderingValue OrderingValue
	);

	public IEnumerable<Entry> Entries
		=> this.EntryStorage;

	private readonly List<Entry> EntryStorage = [];

	public bool Ascending;

	public OrderedList() : this(ascending: true) { }

	public OrderedList(bool ascending)
	{
		this.Ascending = ascending;
	}

	public OrderedList(OrderedList<TElement, TOrderingValue> anotherList)
	{
		this.Ascending = anotherList.Ascending;
		this.EntryStorage.AddRange(anotherList.EntryStorage);
	}

	public int Count
		=> this.EntryStorage.Count;

	public TElement this[int index]
		=> this.EntryStorage[index].Element;

	public IEnumerator<TElement> GetEnumerator()
		=> this.EntryStorage.Select(e => e.Element).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> this.GetEnumerator();

	public void Clear()
		=> this.EntryStorage.Clear();

	public bool Contains(TElement item)
		=> this.EntryStorage.Any(e => Equals(e.Element, item));

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
