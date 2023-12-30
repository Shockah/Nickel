using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Nickel;

public sealed class OrderedList<TElement, TOrderingValue> : IReadOnlyList<TElement> where TOrderingValue : IComparable<TOrderingValue>
{
	private record struct Entry(
		TElement Element,
		TOrderingValue OrderingValue
	);

	private readonly List<Entry> Entries = [];

	public int Count
		=> this.Entries.Count;

	public TElement this[int index]
		=> this.Entries[index].Element;

	public IEnumerator<TElement> GetEnumerator()
		=> this.Entries.Select(e => e.Element).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> this.GetEnumerator();

	public void Clear()
		=> this.Entries.Clear();

	public bool Contains(TElement item)
		=> this.Entries.Any(e => Equals(e.Element, item));

	public void Add(TElement element, TOrderingValue orderingValue)
	{
		for (var i = 0; i < this.Entries.Count; i++)
		{
			if (this.Entries[i].OrderingValue.CompareTo(orderingValue) > 0)
			{
				this.Entries.Insert(i, new(element, orderingValue));
				return;
			}
		}
		this.Entries.Add(new(element, orderingValue));
	}

	public bool Remove(TElement element)
	{
		for (var i = 0; i < this.Entries.Count; i++)
		{
			if (Equals(this.Entries[i].Element, element))
			{
				this.Entries.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	public bool TryGetOrderingValue(TElement element, [MaybeNullWhen(false)] out TOrderingValue orderingValue)
	{
		foreach (var entry in this.Entries)
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
