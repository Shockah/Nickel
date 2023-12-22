using OneOf;
using OneOf.Types;

namespace Nickel;

internal interface ICobaltCoreResolver
{
    OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore();
}
