using Nanoray.Pintail;
using System;

namespace Nickel;

/// <summary>
/// A mod-specific accessor for various utilities.
/// </summary>
public interface IModUtilities
{
	/// <summary>
	/// Obtains a new unique case value for the given enum type.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <returns>A new unique case value for the given enum type.</returns>
	T ObtainEnumCase<T>() where T : struct, Enum;

	/// <summary>
	/// Frees the given enum case value that was previously obtained via <see cref="ObtainEnumCase{T}"/>.
	/// This allows the value to be reused.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="case">The enum case value to free.</param>
	void FreeEnumCase<T>(T @case) where T : struct, Enum;
	
	/// <summary>The manager used to proxy between types to allow cross-mod communication without hard assembly references.</summary>
	/// <seealso cref="IModRegistry.GetApi{TApi}"/>
	IProxyManager<string> ProxyManager { get; }

	/// <summary>
	/// Reverses any proxies done via <see cref="ProxyManager"/>, returning the original target.
	/// </summary>
	/// <param name="potentialProxy">A potential proxy.</param>
	/// <returns>A fully unproxied original target, or the passed in object if it is not a proxy.</returns>
	object Unproxy(object potentialProxy);
	
	/// <summary>
	/// The <see cref="Harmony"/> instance for this mod, used for patching methods.
	/// </summary>
	IHarmony Harmony { get; }
}
