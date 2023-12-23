using System;
using Microsoft.Extensions.Logging;

namespace Nickel;

internal sealed class ModEventManager
{
    public ManagedEvent<Nothing> OnAllModsLoadedManagedEvent { get; private init; }

    public ModEventManager(Func<IModManifest, ILogger> loggerProvider)
    {
        this.OnAllModsLoadedManagedEvent = new((handler, mod, exception) =>
        {
            var logger = loggerProvider(mod);
            logger.LogError("Mod failed in event `OnAllModsLoaded`: {Exception}", exception);
        });
    }
}
