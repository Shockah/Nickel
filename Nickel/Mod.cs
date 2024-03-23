namespace Nickel;

/// <summary>
/// Describes a single mod.<br/>
/// Nickel constructs <see cref="Mod"/> instances by finding constructors with parameters it can inject with its DI container.
/// </summary>
public abstract class Mod
{
	/// <summary>
	/// Provides an API object to use for communicating between mods, without introducing hard code references.<br/>
	/// All <c>public</c> methods of the returned object will be accessible. The API object's type has to be <c>public</c>.
	/// </summary>
	/// <param name="requestingMod">The mod that requested the API.</param>
	/// <returns>The API object to return, or <c>null</c> if none.</returns>
	public virtual object? GetApi(IModManifest requestingMod)
		=> null;
}
