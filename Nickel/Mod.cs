namespace Nickel;

/**
 * <summary>Represents a mod.</summary>
 * <remarks>
 * This is the entrypoint type for all mods.
 * Access to various parts of the modloader infrastructure is provided via injected constructor parameters.
 * </remarks>
 */
public abstract class Mod
{
	/**
	 * <summary>Returns the API implementation for this mod.</summary>
	 * <remarks>This is proxied to the requesting mod via Pintail.</remarks>#
	 * <seealso cref="IModRegistry.GetApi{TApi}" />
	 */
	public virtual object? GetApi(IModManifest requestingMod)
		=> null;
}
