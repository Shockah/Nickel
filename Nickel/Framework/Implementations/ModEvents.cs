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

    public event EventHandler<Nothing> OnAllModsLoaded
    {
        add => this.EventManager.OnAllModsLoadedManagedEvent.Add(value, this.ModManifest);
        remove => this.EventManager.OnAllModsLoadedManagedEvent.Remove(value, this.ModManifest);
    }
}
