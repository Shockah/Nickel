using Microsoft.Extensions.Logging;
using Shockah.PluginManager;

namespace Nickel;

public abstract class Mod
{
    public IPluginPackage<IModManifest> Package { get; internal set; } = null!;
    public IModHelper Helper { get; internal set; } = null!;
    public ILogger Logger { get; internal set; } = null!;

    public virtual object? GetApi(IModManifest requestingMod)
        => null;
}
