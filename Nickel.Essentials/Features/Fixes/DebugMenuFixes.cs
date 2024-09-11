using HarmonyLib;
using System.Linq;
using System.Reflection;

namespace Nickel.Essentials;

internal static class DebugMenuFixes
{
	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredConstructor(typeof(Editor), []),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Editor_ctor_Postfix))
		);

	private static void Editor_ctor_Postfix(Editor __instance)
		=> __instance.allDecks = __instance.allDecks
			.Concat(DB.decks.Keys)
			.Distinct()
			.ToList();
}
