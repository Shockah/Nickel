namespace Nickel;

public abstract class Mod
{
	public virtual object? GetApi(IModManifest requestingMod)
		=> null;
}
