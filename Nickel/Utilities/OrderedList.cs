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

    private readonly List<Entry> Entries = new();

    public int Count
        => Entries.Count;

    public TElement this[int index]
        => Entries[index].Element;

    public IEnumerator<TElement> GetEnumerator()
        => Entries.Select(e => e.Element).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public void Clear()
        => Entries.Clear();

    public bool Contains(TElement item)
        => Entries.Any(e => Equals(e.Element, item));

    public void Add(TElement element, TOrderingValue orderingValue)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].OrderingValue.CompareTo(orderingValue) > 0)
            {
                Entries.Insert(i, new(element, orderingValue));
                return;
            }
        }
        Entries.Add(new(element, orderingValue));
    }

    public bool Remove(TElement element)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            if (Equals(Entries[i].Element, element))
            {
                Entries.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public bool TryGetOrderingValue(TElement element, [MaybeNullWhen(false)] out TOrderingValue orderingValue)
    {
        foreach (var entry in Entries)
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
