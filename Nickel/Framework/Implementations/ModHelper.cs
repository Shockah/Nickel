namespace Nickel;

internal sealed class ModHelper : IModHelper
{
    public IModRegistry ModRegistry { get; init; }

    public ModHelper(IModRegistry modRegistry)
    {
        this.ModRegistry = modRegistry;
    }
}
