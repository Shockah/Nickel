using System;
using System.Collections;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Describes an unordered pair of elements.
/// </summary>
/// <typeparam name="T">The type of elements.</typeparam>
public readonly struct UnorderedPair<T> : IEquatable<UnorderedPair<T>>, IEnumerable<T>
{
	/// <summary>
	/// The first of the two values stored by this pair.
	/// </summary>
	public T First { get; init; }

	/// <summary>
	/// The second of the two values stored by this pair.
	/// </summary>
	public T Second { get; init; }

	/// <summary>
	/// Creates a new unordered pair.
	/// </summary>
	/// <param name="first">The first of the two values stored by this pair.</param>
	/// <param name="second">The second of the two values stored by this pair.</param>
	public UnorderedPair(T first, T second)
	{
		this.First = first;
		this.Second = second;
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
		=> obj is UnorderedPair<T> pair && this.Equals(pair);

	/// <inheritdoc/>
	public bool Equals(UnorderedPair<T> other)
		=> (Equals(this.First, other.First) && Equals(this.Second, other.Second)) || (Equals(this.First, other.Second) && Equals(this.Second, other.First));

	/// <inheritdoc/>
	public override int GetHashCode()
		=> (this.First?.GetHashCode() ?? 0) ^ (this.Second?.GetHashCode() ?? 0);

	/// <inheritdoc/>
	public IEnumerator<T> GetEnumerator()
		=> ((IEnumerable<T>)[this.First, this.Second]).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> this.GetEnumerator();

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public static bool operator ==(UnorderedPair<T> left, UnorderedPair<T> right)
		=> Equals(left, right);

	public static bool operator !=(UnorderedPair<T> left, UnorderedPair<T> right)
		=> !Equals(left, right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
