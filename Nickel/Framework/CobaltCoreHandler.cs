using System.Reflection;
using OneOf;
using OneOf.Types;

namespace Nickel;

internal sealed class CobaltCoreHandler
{
    private ICobaltCoreResolver Resolver { get; init; }

    public CobaltCoreHandler(ICobaltCoreResolver resolver)
    {
        this.Resolver = resolver;
    }

    public OneOf<CobaltCoreHandlerResult, Error<string>> SetupGame()
    {
        var resolveResultOrError = this.Resolver.ResolveCobaltCore();
        if (resolveResultOrError.TryPickT1(out var error, out var resolveResult))
            return new Error<string>($"Could not resolve Cobalt Core: {error.Value}");

        var gameAssembly = Assembly.Load(
            rawAssembly: resolveResult.GameAssemblyDataStream.ToMemoryStream().ToArray(),
            rawSymbolStore: resolveResult.GamePdbDataStream?.ToMemoryStream().ToArray()
        );
        return new CobaltCoreHandlerResult { GameAssembly = gameAssembly };
    }
}
