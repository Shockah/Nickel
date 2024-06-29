namespace Nickel;

/// <summary>
/// A mod-specific status registry.
/// Allows looking up and registering statuses.
/// </summary>
public interface IModStatuses
{
	/// <summary>
	/// Lookup a <see cref="Status"/> entry by its enum constant.
	/// </summary>
	/// <param name="status">The enum constant.</param>
	/// <returns>An entry, or <c>null</c> if the type does not match any known statuses.</returns>
	IStatusEntry? LookupByStatus(Status status);
	
	/// <summary>
	/// Lookup a <see cref="Status"/> entry by its full <see cref="IModOwned.UniqueName"/>.
	/// </summary>
	/// <param name="uniqueName">The unique name to retrieve an entry for.</param>
	/// <returns>An entry, or <c>null</c> if the unique name does not match any known statuses.</returns>
	IStatusEntry? LookupByUniqueName(string uniqueName);
	
	/// <summary>
	/// Register a new <see cref="Status"/>.
	/// </summary>
	/// <param name="name">The local (mod-level) name for the <see cref="Status"/>. This has to be unique across all statuses in the mod.</param>
	/// <param name="configuration">A configuration describing all aspects of the <see cref="Status"/>.</param>
	/// <returns>An entry for the new <see cref="Status"/>.</returns>
	IStatusEntry RegisterStatus(string name, StatusConfiguration configuration);
}
