using System;

namespace Nickel;

internal sealed class ModHelper : IModHelper
{
    public IModRegistry ModRegistry { get; init; }
    public IModEvents Events { get; init; }

    public IModSprites Sprites
    {
        get
        {
            if (this.CurrentModLoadPhaseProvider() < ModLoadPhase.AfterGameAssembly)
                throw new InvalidOperationException("Cannot access sprites before the game assembly is loaded.");
            return this.SpritesStorage;
        }
    }

    private IModSprites SpritesStorage { get; init; }
    private Func<ModLoadPhase> CurrentModLoadPhaseProvider { get; init; }

    public ModHelper(IModRegistry modRegistry, IModEvents events, IModSprites sprites, Func<ModLoadPhase> currentModLoadPhaseProvider)
    {
        this.ModRegistry = modRegistry;
        this.Events = events;
        this.SpritesStorage = sprites;
        this.CurrentModLoadPhaseProvider = currentModLoadPhaseProvider;
    }
}
