using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Bugfixes;

internal static class BlendStateFixes
{
	private static readonly BlendState ExpectedNonPremultiplied = new BlendState
	{
		ColorSourceBlend = Blend.SourceAlpha,
		ColorDestinationBlend = Blend.InverseSourceAlpha,
		AlphaSourceBlend = Blend.One,
		AlphaDestinationBlend = Blend.InverseSourceAlpha,
	};
	
	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Draw), nameof(Draw.SetBlendMode))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Draw)}.{nameof(Draw.SetBlendMode)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Draw_SetBlendMode_Transpiler))
		);

	private static IEnumerable<CodeInstruction> Draw_SetBlendMode_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Ldsfld("NonPremultiplied"))
				.Replace(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ExpectedNonPremultiplied))))
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}
}
