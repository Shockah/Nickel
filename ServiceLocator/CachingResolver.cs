using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.ServiceLocator;

public sealed class CachingResolver(
	IResolver resolver
) : IResolver
{
	private readonly Dictionary<Type, object?> Cache = [];

	/// <inheritdoc/>
	public bool CanResolve<TComponent>(IResolver rootResolver)
	{
		if (this.Cache.ContainsKey(typeof(TComponent)))
			return true;
		return resolver.CanResolve<TComponent>();
	}

	/// <inheritdoc/>
	public bool TryResolve<TComponent>(IResolver rootResolver, [MaybeNullWhen(false)] out TComponent component)
	{
		if (this.Cache.TryGetValue(typeof(TComponent), out var rawComponent))
		{
			component = (TComponent)rawComponent!;
			return true;
		}
		if (resolver.TryResolve<TComponent>(rootResolver, out var wrappedComponent))
		{
			component = wrappedComponent;
			this.Cache[typeof(TComponent)] = wrappedComponent;
			return true;
		}

		component = default;
		return false;
	}
}
