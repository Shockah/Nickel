using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nickel;

internal static class TTGlossaryPatches
{
	internal static RefEventHandler<TryGetIconEventArgs>? OnTryGetIcon;

	private static readonly Stack<TTGlossary> GlossaryStack = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(TTGlossary), nameof(TTGlossary.BuildIconAndText))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(TTGlossary)}.{nameof(TTGlossary.BuildIconAndText)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(BuildIconAndText_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(BuildIconAndText_Finalizer))
		);
		harmony.Patch(
		   original: AccessTools.DeclaredMethod(typeof(TTGlossary), nameof(TTGlossary.TryGetIcon))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(TTGlossary)}.{nameof(TTGlossary.TryGetIcon)}`"),
		   postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(TryGetIcon_Postfix))
	   );
	}

	private static void BuildIconAndText_Prefix(TTGlossary __instance)
		=> GlossaryStack.Push(__instance);

	private static void BuildIconAndText_Finalizer()
		=> GlossaryStack.Pop();

	private static void TryGetIcon_Postfix(ref Spr? __result)
	{
		if (!GlossaryStack.TryPeek(out var glossary))
			return;
		
		var args = new TryGetIconEventArgs
		{
			Glossary = glossary,
			Sprite = __result,
		};
		OnTryGetIcon?.Invoke(null, ref args);
		__result = args.Sprite;
	}

	internal struct TryGetIconEventArgs
	{
		public required TTGlossary Glossary { get; init; }
		public required Spr? Sprite;
	}
}
