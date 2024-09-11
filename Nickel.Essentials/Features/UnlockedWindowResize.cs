using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;

namespace Nickel.Essentials;

internal static class UnlockedWindowResize
{
	private static bool ForceUpdateOnce = true;
	private static bool UserResized = true;
	private static bool RepositionOnce;

	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MG), nameof(MG.UpdateRenderPipelineIfNeeded))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(MG)}.{nameof(MG.UpdateRenderPipelineIfNeeded)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(MG_UpdateRenderPipelineIfNeeded_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(MG_UpdateRenderPipelineIfNeeded_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.LetterboxTransform))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(G)}.{nameof(G.LetterboxTransform)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(G_LetterboxTransform_Prefix))
		);
	}

	private static bool MG_UpdateRenderPipelineIfNeeded_Prefix(MG __instance)
	{
		if (ForceUpdateOnce)
		{
			__instance.Window.AllowUserResizing = true;
			__instance.Window.ClientSizeChanged += (_, _) => UserResized = true;
		}

		// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
		var fullscreen = __instance.g.settings?.fullscreen ?? true;
		if (ForceUpdateOnce || fullscreen || fullscreen != __instance.graphics.IsFullScreen)
		{
			ForceUpdateOnce = false;
			if (!fullscreen && fullscreen != __instance.graphics.IsFullScreen)
				RepositionOnce = true;
			return true;
		}

		if (!UserResized)
			return false;
		UserResized = false;

		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (__instance.renderTarget is not null && !__instance.renderTarget.IsDisposed && __instance.GraphicsDevice.PresentationParameters.BackBufferWidth == __instance.renderTarget.Width && __instance.GraphicsDevice.PresentationParameters.BackBufferHeight == __instance.renderTarget.Height)
			return false;

		var scale = Math.Min(
			__instance.GraphicsDevice.PresentationParameters.BackBufferWidth / __instance.PIX_W,
			__instance.GraphicsDevice.PresentationParameters.BackBufferHeight / __instance.PIX_H
		);
		__instance.PIX_SCALE = scale;
		
		__instance.renderTarget?.Dispose();
		__instance.renderTarget = new RenderTarget2D(__instance.GraphicsDevice, __instance.PIX_W * scale, __instance.PIX_H * scale);
		__instance.g.e?.UpdateImguiTex(__instance);
		__instance.graphics.ApplyChanges();
		return false;
	}

	private static void MG_UpdateRenderPipelineIfNeeded_Postfix(MG __instance)
	{
		if (!RepositionOnce)
			return;
		RepositionOnce = false;
		
		__instance.Window.Position = new Point(
			GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width / 2 - __instance.graphics.PreferredBackBufferWidth / 2,
			GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height / 2 - __instance.graphics.PreferredBackBufferHeight / 2
		);
		__instance.graphics.ApplyChanges();
	}

	private static void G_LetterboxTransform_Prefix(out int destW, out int destH)
	{
		destW = MG.inst.GraphicsDevice.PresentationParameters.BackBufferWidth;
		destH = MG.inst.GraphicsDevice.PresentationParameters.BackBufferHeight;
	}
}
