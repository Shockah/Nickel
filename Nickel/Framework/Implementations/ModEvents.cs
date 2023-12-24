using System;

namespace Nickel;

internal sealed class ModEvents : IModEvents
{
    private IModManifest ModManifest { get; init; }
    private ModEventManager EventManager { get; init; }

    public ModEvents(IModManifest modManifest, ModEventManager eventManager)
    {
        this.ModManifest = modManifest;
        this.EventManager = eventManager;
    }

    public event EventHandler<ModLoadPhase> OnModLoadPhaseFinished
    {
        add => this.EventManager.OnModLoadPhaseFinishedEvent.Add(value, this.ModManifest);
        remove => this.EventManager.OnModLoadPhaseFinishedEvent.Remove(value, this.ModManifest);
    }
}
