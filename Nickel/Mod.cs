using Microsoft.Extensions.Logging;

namespace Nickel;

public abstract class Mod
{
    public IModManifest Manifest { get; internal set; } = null!;
    public IModHelper Helper { get; internal set; } = null!;
    public ILogger Logger { get; internal set; } = null!;

    public virtual object? GetApi(IModManifest requestingMod)
        => null;
}
