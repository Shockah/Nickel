using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Nanoray.ServiceLocator;

public sealed class ExtendableResolver : IResolver
{
	private readonly List<IResolver> Resolvers = [];

	/// <inheritdoc/>
	public bool CanResolve<TComponent>(IResolver rootResolver)
		=> this.Resolvers.Any(r => r.CanResolve<TComponent>(rootResolver));

	/// <inheritdoc/>
	public bool TryResolve<TComponent>(IResolver rootResolver, [MaybeNullWhen(false)] out TComponent component)
	{
		foreach (var resolver in this.Resolvers)
			if (resolver.TryResolve(rootResolver, out component))
				return true;

		component = default;
		return false;
	}
	
	/// <summary>
	/// Register a resolver.
	/// </summary>
	/// <param name="resolver">The resolver.</param>
	public void RegisterResolver(IResolver resolver)
		=> this.Resolvers.Add(resolver);

	/// <summary>
	/// Unregister a resolver.
	/// </summary>
	/// <param name="resolver">The resolver.</param>
	public void UnregisterResolver(IResolver resolver)
		=> this.Resolvers.Remove(resolver);
}
