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
	private static readonly UK ExportProfileKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	private static readonly UK ImportProfileBackKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	private static readonly UK WarningPopupKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

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
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static string ProfileSelect_Render_Transpiler_ModifyTitle(string text, ProfileSelect self)
		=> self is SaveImportRoute route ? ModEntry.Instance.Localizations.Localize([route.ToExport is null ? "saveImport" : "saveExport", "title"]) : text;
	
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
		if (slot.isCorrupted)
			return;
		
		if (string.IsNullOrEmpty(slot.state?.GetSlotSummary()))
			__result = __result.Append(new UIKey(ImportProfileKey, n));
		else
			__result = __result.Append(new UIKey(ExportProfileKey, n));
	}

	private static void ProfileSelect_RenderActionButton_Postfix(ProfileSelect __instance, G g, UIKey actionKey, Vec pos)
	{
		if (actionKey.k == ImportProfileKey)
		{
			SharedArt.ButtonText(g, pos, actionKey, ModEntry.Instance.Localizations.Localize(["saveImport", "button"]), onMouseDown: new MouseDownHandler(() =>
			{
				Audio.Play(Event.Click);
				ModEntry.Instance.Helper.ModData.SetOptionalModData(__instance, "Subroute", new SaveImportRoute { ToExport = null, TargetSlot = actionKey.v });
			}));
		}
		else if (actionKey.k == ExportProfileKey)
		{
			SharedArt.ButtonText(g, pos, actionKey, ModEntry.Instance.Localizations.Localize(["saveExport", "button"]), onMouseDown: new MouseDownHandler(() =>
			{
				if (__instance._slotsCache is null || __instance._slotsCache.Count <= actionKey.v || __instance._slotsCache[actionKey.v].isCorrupted || __instance._slotsCache[actionKey.v].state is not { } state)
				{
					Audio.Play(Event.ZeroEnergy);
					return;
				}
				
				Audio.Play(Event.Click);
				ModEntry.Instance.Helper.ModData.SetOptionalModData(__instance, "Subroute", new SaveImportRoute { ToExport = state, TargetSlot = actionKey.v });
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
		public required State? ToExport;
		public required int TargetSlot;
		private (string Text, double Time)? Warning;

		public override void Render(G g)
		{
			base.Render(g);
			SharedArt.ButtonText(g, new Vec(413, 228), ImportProfileBackKey, Loc.T("uiShared.btnBack"), onMouseDown: this, platformButtonHint: Btn.B);

			if (this.Warning is { } warning)
			{
				SharedArt.WarningPopup(g, WarningPopupKey, warning.Text, new Vec(240, 65));

				var warningTime = Math.Max(0, warning.Time - g.dt);
				this.Warning = warningTime <= 0 ? null : warning with { Time = warningTime };
			}
		}

		void OnMouseDown.OnMouseDown(G g, Box b)
		{
			if (b.key?.ValueFor(StableUK.profile_select) is { } slot)
			{
				this.HandleSlot(g, slot);
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
			else if (Input.GetGpDown(Btn.A) && b.key?.ValueFor(StableUK.profile_select) is { } slot)
				this.HandleSlot(g, slot);
			else
				this.OnInputPhase(g, b);
		}

		private void HandleSlot(G g, int slot)
		{
			if (this.ToExport is null)
				this.HandleSlotImport(g, slot);
			else
				this.HandleSlotExport(g, slot);
		}

		private void HandleSlotImport(G g, int slot)
		{
			if (this._slotsCache is null || this._slotsCache.Count <= slot || this._slotsCache[slot].isCorrupted || this._slotsCache[slot].state is not { } state)
			{
				Audio.Play(Event.ZeroEnergy);
				return;
			}

			var vanillaSavePath = "";
			DoWithVanillaSavePath(() => vanillaSavePath = Storage.SavePath(State.GetSavePath(slot)));
			var moddedSavePath = Storage.SavePath(State.GetSavePath(this.TargetSlot));

			vanillaSavePath = new FileInfo(vanillaSavePath).Directory!.FullName;
			moddedSavePath = new FileInfo(moddedSavePath).Directory!.FullName;

			Audio.Play(Event.Click);
				
			if (Directory.Exists(moddedSavePath))
				foreach (var filePath in Directory.EnumerateFiles(moddedSavePath, "*", SearchOption.AllDirectories))
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
				if (file.Name is "Save.json" or "BigStats.json")
					continue;

				var relativePath = Path.GetRelativePath(vanillaSavePath, filePath);
				var copyDestination = new FileInfo(Path.Combine(moddedSavePath, relativePath));
				copyDestination.Directory!.Create();
				file.CopyTo(copyDestination.FullName);
			}
		}

		private void HandleSlotExport(G g, int slot)
		{
			if (this.ToExport is not { } state || this._slotsCache is null || this._slotsCache.Count <= slot)
			{
				Audio.Play(Event.ZeroEnergy);
				return;
			}
			
			if (this._slotsCache[slot].isCorrupted || !string.IsNullOrEmpty(this._slotsCache[slot].state?.GetSlotSummary()))
			{
				Audio.Play(Event.ZeroEnergy);
				this.Warning = (ModEntry.Instance.Localizations.Localize(["saveExport", "cannotOverride"]), 2);
				return;
			}
			
			var vanillaSavePath = "";
			DoWithVanillaSavePath(() => vanillaSavePath = Storage.SavePath(State.GetSavePath(slot)));
			// var moddedSavePath = Storage.SavePath(State.GetSavePath(this.TargetSlot));
			
			vanillaSavePath = new FileInfo(vanillaSavePath).Directory!.FullName;
			// moddedSavePath = new FileInfo(moddedSavePath).Directory!.FullName;

			Audio.Play(Event.Click);
				
			if (Directory.Exists(vanillaSavePath))
				foreach (var filePath in Directory.EnumerateFiles(vanillaSavePath, "*", SearchOption.AllDirectories))
					File.Delete(filePath);

			var realState = g.state;
			var stateCopy = Mutil.DeepCopy(state);
			RemoveAllModDataRecursively(stateCopy);
			
			try
			{
				PurgeCustomData();
				if (ShouldResetRun(false))
					ResetRun();
				if (ShouldResetRun(true))
					throw new InvalidOperationException("Attempted to purge custom data from exported save data, but it still contains some");
				
				g.state = stateCopy;
				DoWithVanillaSavePath(() =>
				{
					stateCopy.slot = slot;
					stateCopy.SaveIfRelease();
					
					// TODO: maybe copy run summaries too
				});
			}
			finally
			{
				g.state = realState;
				g.CloseRoute(this);
			}

			bool ShouldResetRun(bool alreadyResetOnce)
			{
				if (stateCopy.ship.key.Contains("::")) // only modded ships do that
					return true;
				if (!alreadyResetOnce && stateCopy.characters.Count == 0)
					return true;
				if (HasCustomTypesOrEnumValuesRecursively(stateCopy))
					return true;
				return false;
			}

			void ResetRun()
			{
				stateCopy.ship = Mutil.DeepCopy(StarterShip.ships["artemis"].ship);
				stateCopy.characters = [];
				stateCopy.artifacts = [];
				stateCopy.map = new MapFirst();
				stateCopy.route = new NewRunOptions();
				stateCopy.routeOverride = null;
				stateCopy.pendingRunSummary = null;
				stateCopy.rewardsQueue = [];
				stateCopy.runConfig.selectedShip = "artemis";
				typeof(State).GetField(nameof(State.temporaryStoryVars))?.SetValue(stateCopy, null);
				typeof(State).GetField(nameof(State.dailyDay))?.SetValue(stateCopy, null);
				stateCopy.map.Populate(stateCopy, stateCopy.rngZone);
			}

			void PurgeCustomData()
			{
				// story vars
				PurgeCustomEnum(ref stateCopy.storyVars.whoDidThat);
				PurgeCustomTypesOrEnumValuesFromSet(stateCopy.storyVars.unlockedChars);
				PurgeCustomTypesOrEnumValuesFromSet(stateCopy.storyVars.statusesPlayerGainedThisTurn);
				PurgeCustomTypesOrEnumValuesFromSet(stateCopy.storyVars.statusesEnemyGainedThisTurn);
				PurgeCustomTypesOrEnumValuesFromList(stateCopy.storyVars.unlockedCharsToAnnounce);
				PurgeCustomTypesOrEnumValuesFromDictionaryKeys(stateCopy.storyVars.memoryUnlockLevel);
				stateCopy.storyVars.cardsOwned.RemoveWhere(k => k.Contains("::"));
				stateCopy.storyVars.artifactsOwned.RemoveWhere(k => k.Contains("::"));
				
				// state
				stateCopy.characters = stateCopy.characters.Where(c => c.deckType is { } deckType && Enum.GetValues<Deck>().Contains(deckType)).ToList();
				PurgeCustomTypesOrEnumValuesFromList(stateCopy.rewardsQueue);
				PurgeCustomTypesOrEnumValuesFromList(stateCopy.deck);
				PurgeCustomTypesOrEnumValuesFromList(stateCopy.artifacts);
				foreach (var character in stateCopy.characters)
					PurgeCustomTypesOrEnumValuesFromList(character.artifacts);
				
				// ship
				PurgeCustomTypesOrEnumValuesFromDictionaryKeys(stateCopy.ship.statusEffects);
				PurgeCustomTypesOrEnumValuesFromDictionaryKeys(stateCopy.ship.statusEffectPulses);
				
				// combat
				if (stateCopy.route is Combat combat)
				{
					PurgeCustomTypesOrEnumValuesFromList(combat.hand);
					PurgeCustomTypesOrEnumValuesFromList(combat.discard);
					PurgeCustomTypesOrEnumValuesFromList(combat.exhausted);
					PurgeCustomTypesOrEnumValuesFromList(combat.cardActions);
					
					// enemy ship
					PurgeCustomTypesOrEnumValuesFromDictionaryKeys(combat.otherShip.statusEffects);
					PurgeCustomTypesOrEnumValuesFromDictionaryKeys(combat.otherShip.statusEffectPulses);
				}
			}
		}

		private static void PurgeCustomEnum<T>(ref T? current, T? @default = null) where T : struct, Enum
			=> current = current is null ? null : (Enum.GetValues<T>().Contains(current.Value) ? current : @default);

		// private static void PurgeCustomEnum<T>(ref T current, T @default = default) where T : struct, Enum
		// 	=> current = Enum.GetValues<T>().Contains(current) ? current : @default;

		private static void PurgeCustomTypesOrEnumValuesFromDictionaryKeys<TKey, TValue>(Dictionary<TKey, TValue> dictionary) where TKey : notnull
		{
			foreach (var key in dictionary.Keys.ToList())
				if (HasCustomTypesOrEnumValuesRecursively(key))
					dictionary.Remove(key);
		}

		private static void PurgeCustomTypesOrEnumValuesFromSet<T>(HashSet<T> set)
		{
			foreach (var value in set.ToList())
				if (value is not null && HasCustomTypesOrEnumValuesRecursively(value))
					set.Remove(value);
		}

		private static void PurgeCustomTypesOrEnumValuesFromList<T>(List<T> list)
		{
			for (var i = list.Count - 1; i >= 0; i--)
				if (list[i] is not null && HasCustomTypesOrEnumValuesRecursively(list[i]!))
					list.RemoveAt(i);
		}

		private static bool HasCustomTypesOrEnumValuesRecursively(object o, HashSet<object>? visitedObjects = null)
		{
			visitedObjects ??= [];
			if (!visitedObjects.Add(o))
				return false;
			
			var oType = o.GetType();
			var oAssembly = oType.Assembly;
			var ccAssembly = typeof(Card).Assembly;
			if (oAssembly != ccAssembly && !ccAssembly.GetReferencedAssemblies().Any(a => AssemblyName.ReferenceMatchesDefinition(oAssembly.GetName(), a)))
				return true;
			if (IsCustomEnumValue())
				return true;

			foreach (var field in oType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				if (field.GetValue(o) is { } fieldValue)
					if (HasCustomTypesOrEnumValuesRecursively(fieldValue, visitedObjects))
						return true;

			return false;

			bool IsCustomEnumValue()
			{
				if (!oType.IsEnum)
					return false;
				if (oType.GetCustomAttribute(typeof(FlagsAttribute)) is not null)
					return false;
				
				var enumValues = Enum.GetValues(oType);
				for (var i = 0; i < enumValues.Length; i++)
					if (o.Equals(enumValues.GetValue(i)))
						return false;
				return true;
			}
		}

		private static void RemoveAllModDataRecursively(object o, HashSet<object>? visitedObjects = null)
		{
			visitedObjects ??= [];
			
			var oType = o.GetType();
			if (oType.IsValueType)
				return;
			
			if (!visitedObjects.Add(o))
				return;
			ModEntry.Instance.Helper.ModData.RemoveAllModData(o);

			foreach (var field in oType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				if (field.GetValue(o) is { } fieldValue)
					RemoveAllModDataRecursively(fieldValue, visitedObjects);
		}
	}
}
