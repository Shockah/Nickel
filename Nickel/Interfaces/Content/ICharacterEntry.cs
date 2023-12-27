namespace Nickel;

public interface ICharacterEntry : IModOwned
{
    CharacterConfiguration Configuration { get; }
}
