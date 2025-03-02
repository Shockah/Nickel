using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;

namespace Nickel.Bugfixes;

internal static class SpriteCulling
{
	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Draw), nameof(Draw.Sprite), [typeof(Texture2D), typeof(double), typeof(double), typeof(bool), typeof(bool), typeof(double), typeof(Vec?), typeof(Vec?), typeof(Vec?), typeof(Rect?), typeof(Color?), typeof(BlendState), typeof(SamplerState), typeof(Effect)])
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Draw)}.{nameof(Draw.Sprite)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Draw_Sprite_Prefix))
		);

	private static bool Draw_Sprite_Prefix(Texture2D? sprite, double x, double y, double rotation, Vec? rotationOrigin, Vec? rotationOriginRelative, Vec? scale, Rect? pixelRect)
	{
		if (sprite is null)
			return false;
		if (MG.inst is not { } mg)
			return true;
		// ReSharper disable once ConstantConditionalAccessQualifier
		if (mg?.renderTarget is null)
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
		
		return bbox.x < mg.PIX_W && bbox.x2 > 0 && bbox.y < mg.PIX_H && bbox.y2 > 0;
	}
}
