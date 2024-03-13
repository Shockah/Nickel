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
}
