using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

internal static class SaveImport
{
	private const UK ImportProfileKey = (UK)2136011;
	private const UK ImportProfileBackKey = (UK)2136012;

	public static void ApplyPatches(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ProfileSelect), nameof(ProfileSelect.ReloadSlotsCache)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(ProfileSelect_ReloadSlotsCache_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ProfileSelect), nameof(ProfileSelect.OnMouseDown)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(ProfileSelect_OnMouseDown_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ProfileSelect), nameof(ProfileSelect.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(ProfileSelect_Render_Prefix)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(ProfileSelect_Render_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ProfileSelect), nameof(ProfileSelect.MkSlot)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(ProfileSelect_MkSlot_Postfix)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(ProfileSelect_MkSlot_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Route), nameof(Route.TryCloseSubRoute)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(Route_TryCloseSubRoute_Prefix))
		);
	}

	private static void DoWithVanillaSavePath(Action code)
	{
		var oldOverrideSaveLocation = FeatureFlags.OverrideSaveLocation;
		var oldDebug = FeatureFlags.Debug;

		FeatureFlags.OverrideSaveLocation = null;
		FeatureFlags.Debug = false;

		try
		{
			code();
		}
		finally
		{
			FeatureFlags.OverrideSaveLocation = oldOverrideSaveLocation;
			FeatureFlags.Debug = oldDebug;
		}
	}

	private static bool ProfileSelect_ReloadSlotsCache_Prefix(ProfileSelect __instance)
	{
		if (__instance is not SaveImportRoute route)
			return true;

		DoWithVanillaSavePath(() =>
		{
			route._slotsCache = Enumerable.Range(0, 3)
				.Select(State.Load)
				.ToList();
		});
		return false;
	}

	private static bool ProfileSelect_OnMouseDown_Prefix(ProfileSelect __instance, G g, Box b)
	{
		if (__instance is SaveImportRoute)
			return true;
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<Route>(__instance, "Subroute") is not { } subroute)
			return true;
		if (subroute is not OnMouseDown handler)
			return true;

		handler.OnMouseDown(g, b);
		return false;
	}

	private static bool ProfileSelect_Render_Prefix(ProfileSelect __instance, G g)
	{
		if (__instance is SaveImportRoute)
			return true;
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<Route>(__instance, "Subroute") is not { } subroute)
			return true;

		subroute.Render(g);
		return false;
	}

	private static IEnumerable<CodeInstruction> ProfileSelect_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldstr("profileSelect.title"),
					ILMatches.Ldstr("SELECT PROFILE"),
					ILMatches.Call("T")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(ProfileSelect_Render_Transpiler_ModifyTitle)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static string ProfileSelect_Render_Transpiler_ModifyTitle(string text, ProfileSelect self)
		=> self is SaveImportRoute ? ModEntry.Instance.Localizations.Localize(["saveImport", "title"]) : text;

	private static void ProfileSelect_MkSlot_Postfix(ProfileSelect __instance, Vec localV, G g, State.SaveSlot st, int n)
	{
		if (__instance is SaveImportRoute)
			return;
		if (st.isCorrupted || !string.IsNullOrEmpty(st.state?.GetSlotSummary()))
			return;

		SharedArt.ButtonText(
			g,
			localV + new Vec(210),
			new UIKey(ImportProfileKey, n),
			ModEntry.Instance.Localizations.Localize(["saveImport", "button"]),
			onMouseDown: new MouseDownHandler(() =>
			{
				Audio.Play(Event.Click);
				ModEntry.Instance.Helper.ModData.SetOptionalModData(__instance, "Subroute", new SaveImportRoute { TargetSlot = n });
			})
		);
	}

	private static IEnumerable<CodeInstruction> ProfileSelect_MkSlot_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(3),
					ILMatches.Ldfld(nameof(State.SaveSlot.state)),
					ILMatches.Brtrue,
					ILMatches.Ldarg(3),
					ILMatches.Ldfld(nameof(State.SaveSlot.isCorrupted)),
					ILMatches.Brfalse.GetBranchTarget(out var branchTarget)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Isinst, typeof(SaveImportRoute)),
					new CodeInstruction(OpCodes.Brtrue, branchTarget.Value)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static bool Route_TryCloseSubRoute_Prefix(Route __instance, ref bool __result)
	{
		if (__instance is not ProfileSelect route)
			return true;
		if (route is SaveImportRoute)
			return true;

		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<Route>(route, "Subroute") is null)
			return true;

		ModEntry.Instance.Helper.ModData.RemoveModData(route, "Subroute");
		__result = true;
		return false;
	}

	private sealed class SaveImportRoute : ProfileSelect, OnMouseDown
	{
		public required int TargetSlot;

		public override void Render(G g)
		{
			base.Render(g);
			SharedArt.ButtonText(g, new Vec(413, 228), ImportProfileBackKey, Loc.T("uiShared.btnBack"), onMouseDown: this, platformButtonHint: Btn.B);
		}

		void OnMouseDown.OnMouseDown(G g, Box b)
		{
			if (b.key?.ValueFor(StableUK.profile_select) is { } slot)
			{
				if (this._slotsCache is null || this._slotsCache.Count <= slot || this._slotsCache[slot].isCorrupted || this._slotsCache[slot].state is not { } state)
				{
					Audio.Play(Event.ZeroEnergy);
					return;
				}

				var vanillaSavePath = "";
				DoWithVanillaSavePath(() => vanillaSavePath = Storage.SavePath(State.GetSavePath(slot)));
				var targetSavePath = Storage.SavePath(State.GetSavePath(this.TargetSlot));

				vanillaSavePath = new FileInfo(vanillaSavePath).Directory!.FullName;
				targetSavePath = new FileInfo(targetSavePath).Directory!.FullName;

				Audio.Play(Event.Click);

				state.slot = this.TargetSlot;
				g.settings.saveSlot = this.TargetSlot;
				g.settings.Save();
				PFX.ClearAll();
				g.state = state;
				Cheevos.CheckOnLoad(g.state);
				g.state.SaveIfRelease();
				g.metaRoute = new MainMenu();

				foreach (var filePath in Directory.EnumerateFiles(vanillaSavePath, "*", SearchOption.AllDirectories))
				{
					var file = new FileInfo(filePath);
					if (file.Name == "Save.json")
						continue;

					var relativePath = Path.GetRelativePath(vanillaSavePath, filePath);
					var copyDestination = new FileInfo(Path.Combine(targetSavePath, relativePath));
					copyDestination.Directory!.Create();
					file.CopyTo(copyDestination.FullName);
				}
			}
			else if (b.key == ImportProfileBackKey)
			{
				Audio.Play(Event.Click);
				g.CloseRoute(this);
			}
			else
			{
				this.OnMouseDown(g, b);
				return;
			}
		}
	}
}
