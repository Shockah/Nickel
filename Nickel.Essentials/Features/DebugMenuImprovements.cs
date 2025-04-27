using HarmonyLib;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

internal static class DebugMenuImprovements
{
	private static bool DidForceWindowSizeOnce;
	private static Vector2 WindowSize = new(550, 600);

	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Editor), nameof(Editor.ImGuiLayout)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Editor_ImGuiLayout_Transpiler))
		);

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Editor_ImGuiLayout_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				// remove `NoResize` flag
				.Find([
					ILMatches.LdcI4(2),
					ILMatches.Call("Begin")
				])
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.Replace(new CodeInstruction(OpCodes.Ldc_I4_0))
				
				// remove forced early resize
				.Find([
					ILMatches.LdcR4(550),
					ILMatches.LdcR4(600),
					ILMatches.Newobj(AccessTools.DeclaredConstructor(typeof(Vector2), [typeof(float), typeof(float)])),
					ILMatches.Call("SetWindowSize"),
				])
				.Remove()
				
				// custom resize handling
				.Find(ILMatches.Call("EndTabBar"))
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Editor_ImGuiLayout_Transpiler_HandleResize)))
				])
				
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void Editor_ImGuiLayout_Transpiler_HandleResize()
	{
		if (ImGui.IsWindowCollapsed())
			return;

		if (DidForceWindowSizeOnce)
		{
			WindowSize = ImGui.GetWindowSize();
			ImGui.SetWindowSize(WindowSize);
		}
		else
		{
			ImGui.SetWindowSize(WindowSize);
			DidForceWindowSizeOnce = true;
		}
	}
}
