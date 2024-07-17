using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nickel;

/// <inheritdoc cref="Harmony"/>
public interface IHarmony
{
	/// <inheritdoc cref="Harmony.Id"/>
	string Id { get; }

	/// <inheritdoc cref="Harmony.Patch"/>
	void Patch(MethodBase original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0);
}
