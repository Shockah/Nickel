using OneOf;
using OneOf.Types;
using System.Collections.Generic;

namespace Nickel;

internal sealed class CompoundCobaltCoreResolver(IReadOnlyList<ICobaltCoreResolver> resolvers) : ICobaltCoreResolver
{
	public OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore()
	{
		List<string> errors = [];
		foreach (var resolver in resolvers)
		{
			var resultOrError = resolver.ResolveCobaltCore();
			if (resultOrError.TryPickT0(out var result, out var error))
				return result;
			errors.Add(error.Value);
		}
		return new Error<string>(string.Join("\n", errors));
	}
}
