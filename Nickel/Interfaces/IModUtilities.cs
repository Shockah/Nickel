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
	/// The <see cref="Harmony"/> instance for this mod, used for patching methods.
	/// </summary>
	IHarmony Harmony { get; }
	
	/// <summary>
	/// The delayed <see cref="Harmony"/> instance for this mod, used for patching methods.<br/>
	/// All patches will be delayed until the <see cref="ModLoadPhase.AfterDbInit"/> phase finishes loading, or until a mod calls <see cref="ApplyDelayedHarmonyPatches"/>.
	/// </summary>
	IHarmony DelayedHarmony { get; }

	/// <summary>
	/// Apply any delayed <see cref="Harmony"/> patches done via the <see cref="DelayedHarmony"/> instance.
	/// </summary>
	void ApplyDelayedHarmonyPatches();
}
