namespace Nickel;

/// <summary>
/// A type that helps with seamless switching of values between profiles.
/// </summary>
/// <typeparam name="TProfile">The profiles type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
public interface IProfileBasedValue<TProfile, TData>
{
	/// <summary>The currently active profile.</summary>
	TProfile ActiveProfile { get; set; }

	/// <summary>The data for the currently active profile (<see cref="ActiveProfile"/>).</summary>
	TData Current { get; set; }

	/// <summary>
	/// Imports data from the given profile to the currently active profile (<see cref="ActiveProfile"/>).
	/// </summary>
	/// <param name="profile">The profile to import data from.</param>
	void Import(TProfile profile);
}
