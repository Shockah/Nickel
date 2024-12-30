using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Input;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

internal static class SaveImport
{
	private static readonly UK ImportProfileKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	private static readonly UK ImportProfileBackKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

	public static void ApplyPatches(IHarmony harmony)
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
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(ProfileSelect_MkSlot_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(ProfileSelect_MkSlot_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ProfileSelect), nameof(ProfileSelect.GetAvailableActionsForSlot)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(ProfileSelect_GetAvailableActionsForSlot_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ProfileSelect), nameof(ProfileSelect.RenderActionButton)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(ProfileSelect_RenderActionButton_Postfix))
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
		// TODO: remove this complete copy-paste of the original method
		// for whatever reason, without this bit the `ProfileSelect_GetAvailableActionsForSlot_Postfix` patch was not called
		if (__instance is not SaveImportRoute route)
		{
			__instance._slotsCache = State.GetSaveSlots();
			__instance._slotsBackupCache = [];
			__instance._availableSlotActionsCache = [];
			for (var i = 0; i < __instance._slotsCache.Count; i++)
			{
				__instance._slotsBackupCache.Add(__instance._slotsCache[i].isCorrupted ? State.LoadFromPath(i, State.GetSaveBackupPath(i)) : null);
				__instance._availableSlotActionsCache.Add(__instance.GetAvailableActionsForSlot(i).ToList());
			}
			return false;
		}

		DoWithVanillaSavePath(() =>
		{
			route._slotsCache = Enumerable.Range(0, 3)
				.Select(State.Load)
				.ToList();
			route._slotsBackupCache = route._slotsCache.Select(State.SaveSlot? (_) => null).ToList();
			route._availableSlotActionsCache = route._slotsCache.Select((_, i) => route.GetAvailableActionsForSlot(i).ToList()).ToList();
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

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
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
	
	private static void ProfileSelect_MkSlot_Prefix(ProfileSelect __instance, G g, ref int __state)
	{
		if (__instance is not SaveImportRoute)
			return;
		__state = g.settings.saveSlot;
		g.settings.saveSlot = -1;
	}
	
	private static void ProfileSelect_MkSlot_Finalizer(ProfileSelect __instance, G g, ref int __state)
	{
		if (__instance is not SaveImportRoute)
			return;
		g.settings.saveSlot = __state;
	}

	private static void ProfileSelect_GetAvailableActionsForSlot_Postfix(ProfileSelect __instance, int n, ref IEnumerable<UIKey> __result)
	{
		if (__instance is SaveImportRoute)
		{
			__result = [];
			return;
		}

		var slot = __instance._slotsCache![n];
		if (slot.isCorrupted || !string.IsNullOrEmpty(slot.state?.GetSlotSummary()))
			return;

		__result = __result.Append(new UIKey(ImportProfileKey, n));
	}

	private static void ProfileSelect_RenderActionButton_Postfix(ProfileSelect __instance, G g, UIKey actionKey, Vec pos)
	{
		if (actionKey.k == ImportProfileKey)
		{
			SharedArt.ButtonText(g, pos, actionKey, ModEntry.Instance.Localizations.Localize(["saveImport", "button"]), onMouseDown: new MouseDownHandler(() =>
			{
				Audio.Play(Event.Click);
				ModEntry.Instance.Helper.ModData.SetOptionalModData(__instance, "Subroute", new SaveImportRoute { TargetSlot = actionKey.v });
			}));
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

	private sealed class SaveImportRoute : ProfileSelect, OnMouseDown, OnInputPhase
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
				
				foreach (var filePath in Directory.EnumerateFiles(targetSavePath, "*", SearchOption.AllDirectories))
					File.Delete(filePath);

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
			}
		}

		void OnInputPhase.OnInputPhase(G g, Box b)
		{
			if (Input.GetGpDown(Btn.B) || Input.GetKeyDown(Keys.Escape))
				g.CloseRoute(this);
			else
				this.OnMouseDown(g, b);
		}
	}
}
