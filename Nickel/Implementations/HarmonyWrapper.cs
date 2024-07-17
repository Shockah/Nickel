using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nickel;

/// <summary>
/// An <see cref="IHarmony"/> implementation that simply wraps a proper <see cref="Harmony"/> instance.
/// </summary>
/// <param name="wrapped">The wrapped <see cref="Harmony"/> instance.</param>
public sealed class HarmonyWrapper(Harmony wrapped) : IHarmony
{
	/// <inheritdoc/>
	public string Id
		=> wrapped.Id;

	/// <inheritdoc/>
	public void Patch(MethodBase original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
		=> wrapped.Patch(original, prefix, postfix, transpiler, finalizer);
}
