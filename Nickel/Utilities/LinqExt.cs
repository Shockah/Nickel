using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

public static class LinqExt
{
	public static T? FirstOrNull<T>(this IEnumerable<T> self) where T : struct
		=> self.Select(e => new T?(e)).FirstOrDefault();

	public static T? FirstOrNull<T>(this IEnumerable<T> self, Func<T, bool> predicate) where T : struct
		=> self.Where(predicate).Select(e => new T?(e)).FirstOrDefault();

	public static T? LastOrNull<T>(this IEnumerable<T> self) where T : struct
		=> self.Select(e => new T?(e)).LastOrDefault();

	public static T? LastOrNull<T>(this IEnumerable<T> self, Func<T, bool> predicate) where T : struct
		=> self.Where(predicate).Select(e => new T?(e)).LastOrDefault();
}
