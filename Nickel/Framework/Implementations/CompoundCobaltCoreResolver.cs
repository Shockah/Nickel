using System.Collections.Generic;
using OneOf;
using OneOf.Types;

namespace Nickel;

internal sealed class CompoundCobaltCoreResolver : ICobaltCoreResolver
{
	private IReadOnlyList<ICobaltCoreResolver> Resolvers { get; init; }

	public CompoundCobaltCoreResolver(IReadOnlyList<ICobaltCoreResolver> resolvers)
	{
		this.Resolvers = resolvers;
	}

	public CompoundCobaltCoreResolver(params ICobaltCoreResolver[] resolvers) : this((IReadOnlyList<ICobaltCoreResolver>)resolvers) { }

	public OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore()
	{
		List<string> errors = new();
		foreach (var resolver in this.Resolvers)
		{
			var resultOrError = resolver.ResolveCobaltCore();
			if (resultOrError.TryPickT0(out var result, out var error))
				return result;
			else
				errors.Add(error.Value);
		}
		return new Error<string>(string.Join("\n", errors));
	}
}
