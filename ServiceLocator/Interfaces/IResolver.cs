using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.ServiceLocator;

public interface IResolver
{
	bool CanResolve<TComponent>()
		=> this.CanResolve<TComponent>(this);
	
	bool CanResolve<TComponent>(IResolver rootResolver);
	
	bool TryResolve<TComponent>([MaybeNullWhen(false)] out TComponent component)
		=> this.TryResolve(this, out component);

	bool TryResolve<TComponent>(IResolver rootResolver, [MaybeNullWhen(false)] out TComponent component);

	TComponent Resolve<TComponent>()
		=> this.Resolve<TComponent>(this);

	TComponent Resolve<TComponent>(IResolver rootResolver)
		=> this.TryResolve<TComponent>(rootResolver, out var component)
			? component
			: throw new ArgumentException($"Tried to resolve an unregistered component of type {typeof(TComponent)}");
}
