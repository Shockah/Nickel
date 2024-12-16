using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nickel;

internal static class TTGlossaryPatches
{
	internal static EventHandler<TryGetIconEventArgs>? OnTryGetIcon;

	private static readonly Stack<TTGlossary> GlossaryStack = new();
	private static readonly Pool<TryGetIconEventArgs> TryGetIconEventArgsPool = new(() => new());

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
		
		var result = __result;
		TryGetIconEventArgsPool.Do(args =>
		{
			args.Glossary = glossary;
			args.Sprite = result;
			OnTryGetIcon?.Invoke(null, args);
			result = args.Sprite;
		});
		__result = result;
	}

	internal sealed class TryGetIconEventArgs
	{
		public TTGlossary Glossary { get; internal set; } = null!;
		public Spr? Sprite { get; set; }
	}
}
