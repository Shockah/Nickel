using System;
using Microsoft.Extensions.Logging;

namespace Nickel;

internal sealed class ModEventManager
{
    public ManagedEvent<ModLoadPhase> OnModLoadPhaseFinishedEvent { get; private init; }

    public ModEventManager(Func<IModManifest, ILogger> loggerProvider)
    {
        this.OnModLoadPhaseFinishedEvent = new((handler, mod, exception) =>
        {
            var logger = loggerProvider(mod);
            logger.LogError("Mod failed in event `OnAllModsLoaded`: {Exception}", exception);
        });
    }
}
