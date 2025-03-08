using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Bugfixes;

internal static class SpriteCulling
{
	private static Matrix FrameStartMatrix;
	
	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.Render))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(G)}.{nameof(G.Render)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(G_Render_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Draw), nameof(Draw.Sprite), [typeof(Texture2D), typeof(double), typeof(double), typeof(bool), typeof(bool), typeof(double), typeof(Vec?), typeof(Vec?), typeof(Vec?), typeof(Rect?), typeof(Color?), typeof(BlendState), typeof(SamplerState), typeof(Effect)])
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Draw)}.{nameof(Draw.Sprite)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Draw_Sprite_Prefix))
		);
	}

	private static IEnumerable<CodeInstruction> G_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Stfld("cameraMatrix"))
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(G_Render_Transpiler_RememberMatrix)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void G_Render_Transpiler_RememberMatrix(G g)
		=> FrameStartMatrix = g.mg.cameraMatrix;

	private static bool Draw_Sprite_Prefix(Texture2D? sprite, double x, double y, double rotation, Vec? rotationOrigin, Vec? rotationOriginRelative, Vec? scale, Rect? pixelRect)
	{
		if (sprite is null)
			return false;
		if (MG.inst is not { } mg)
			return true;
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (mg.renderTarget is null)
			return true;
		
		var renderTargets = MG.inst.GraphicsDevice.GetRenderTargets();
		if (renderTargets.Length == 0)
			return true;
		if (renderTargets[0].RenderTarget is not RenderTarget2D renderTarget || renderTarget != mg.renderTarget)
			return true;

		var nonNullScale = new Vec(Math.Abs(scale?.x ?? 1), Math.Abs(scale?.y ?? 1));
		var bbox = new Rect(x, y, (pixelRect?.w ?? sprite.Width) * nonNullScale.x, (pixelRect?.h ?? sprite.Height) * nonNullScale.y);
		if (rotationOrigin.HasValue)
			bbox = new Rect(bbox.x - rotationOrigin.Value.x * nonNullScale.x, bbox.y - rotationOrigin.Value.y * nonNullScale.y, bbox.w, bbox.h);
		else if (rotationOriginRelative.HasValue)
			bbox = new Rect(bbox.x - rotationOriginRelative.Value.x * bbox.w, bbox.y - rotationOriginRelative.Value.y * bbox.h, bbox.w, bbox.h);
		
		// being lazy with it, just expand the bbox to twice its size
		if (rotation != 0)
			bbox = new Rect(bbox.x - bbox.w, bbox.y - bbox.h, bbox.w * 2, bbox.h * 2);

		if (bbox.x < mg.PIX_W && bbox.x2 > 0 && bbox.y < mg.PIX_H && bbox.y2 > 0)
			return true;
		if (mg.cameraMatrix != FrameStartMatrix)
			return true;
		return false;
	}
}
