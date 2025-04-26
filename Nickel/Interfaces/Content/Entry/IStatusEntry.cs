namespace Nickel;

/// <summary>
/// Describes a <see cref="Status"/>.
/// </summary>
public interface IStatusEntry : IModOwned
{
	/// <summary>The <see cref="Status"/> described by this entry.</summary>
	Status Status { get; }
	
	/// <summary>The configuration used to register the <see cref="Status"/>.</summary>
	StatusConfiguration Configuration { get; }

	/// <summary>
	/// Amends a <see cref="Status"/>' <see cref="StatusConfiguration">configuration</see>.
	/// </summary>
	/// <param name="amends">The amends to make.</param>
	/// <remarks>
	/// This method is only valid for modded entries.
	/// </remarks>
	void Amend(StatusConfiguration.Amends amends);
}
