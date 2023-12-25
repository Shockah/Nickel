namespace Nickel;

public interface IModOwned
{
    IModManifest ModOwner { get; }
    string UniqueName { get; }
}
