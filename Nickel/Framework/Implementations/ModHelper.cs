namespace Nickel;

internal sealed class ModHelper : IModHelper
{
    public IModRegistry ModRegistry { get; init; }
    public IModEvents Events { get; init; }

    public ModHelper(IModRegistry modRegistry, IModEvents events)
    {
        this.ModRegistry = modRegistry;
        this.Events = events;
    }
}
