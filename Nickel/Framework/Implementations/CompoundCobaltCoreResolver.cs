using OneOf;
using OneOf.Types;
using System.Collections.Generic;

namespace Nickel;

internal sealed class CompoundCobaltCoreResolver : ICobaltCoreResolver
{
	private readonly IReadOnlyList<ICobaltCoreResolver> Resolvers;

	public CompoundCobaltCoreResolver(IReadOnlyList<ICobaltCoreResolver> resolvers)
	{
		this.Resolvers = resolvers;
	}

	public OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore()
	{
		List<string> errors = [];
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
