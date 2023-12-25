namespace Nickel;

public interface IModHelper
{
    IModRegistry ModRegistry { get; }
    IModEvents Events { get; }
    IModSprites Sprites { get; }
}
