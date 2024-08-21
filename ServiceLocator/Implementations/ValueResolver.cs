using System.Diagnostics.CodeAnalysis;

namespace Nanoray.ServiceLocator;

public sealed class ValueResolver<T>(
	T value
) : IResolver
{
	/// <inheritdoc/>
	public bool CanResolve<TComponent>(IResolver rootResolver)
		=> typeof(T).IsAssignableTo(typeof(TComponent));

	/// <inheritdoc/>
	public bool TryResolve<TComponent>(IResolver rootResolver, [MaybeNullWhen(false)] out TComponent component)
	{
		if (!typeof(T).IsAssignableTo(typeof(TComponent)))
		{
			component = default!;
			return false;
		}

		component = (TComponent)(object)value!;
		return true;
	}
}
