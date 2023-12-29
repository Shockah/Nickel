using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using WeakEvent;

namespace Nickel;

internal static class TTGlossaryPatches
{
	internal static WeakEventSource<TryGetIconEventArgs> OnTryGetIcon { get; private set; } = new();

	private static readonly Stack<TTGlossary> GlossaryStack = new();

	internal static void Apply(Harmony harmony, ILogger logger)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(TTGlossary), nameof(TTGlossary.BuildIconAndText)) ?? throw new InvalidOperationException("Could not patch game methods: missing method `TTGlossary.BuildIconAndText`"),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(TTGlossaryPatches), nameof(BuildIconAndText_Prefix))),
			finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(TTGlossaryPatches), nameof(BuildIconAndText_Finalizer)))
		);
		harmony.Patch(
		   original: AccessTools.DeclaredMethod(typeof(TTGlossary), "TryGetIcon") ?? throw new InvalidOperationException("Could not patch game methods: missing method `TTGlossary.TryGetIcon`"),
		   postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(TTGlossaryPatches), nameof(TryGetIcon_Postfix)))
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

		TryGetIconEventArgs args = new(glossary, __result);
		OnTryGetIcon.Raise(null, args);
		__result = args.Sprite;
	}

	internal sealed class TryGetIconEventArgs
	{
		public TTGlossary Glossary { get; init; }
		public Spr? Sprite { get; set; }

		public TryGetIconEventArgs(TTGlossary glossary, Spr? sprite)
		{
			this.Glossary = glossary;
			this.Sprite = sprite;
		}
	}
}
