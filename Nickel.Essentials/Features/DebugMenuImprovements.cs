using HarmonyLib;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

internal static class DebugMenuImprovements
{
	private static bool DidForceWindowSizeOnce;
	private static Vector2 WindowSize = new(550, 600);
	private static int TraitCursorPosition;
	private static string TraitSearchText = "";
	private static int? LastHighlightedCardId;

	private static readonly Lazy<List<ICardTraitEntry>> AllTraits = new(() => [
		ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait,
		ModEntry.Instance.Helper.Content.Cards.RetainCardTrait,
		ModEntry.Instance.Helper.Content.Cards.RecycleCardTrait,
		ModEntry.Instance.Helper.Content.Cards.UnplayableCardTrait,
		ModEntry.Instance.Helper.Content.Cards.BuoyantCardTrait,
		ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait,
		ModEntry.Instance.Helper.Content.Cards.SingleUseCardTrait,
		ModEntry.Instance.Helper.Content.Cards.InfiniteCardTrait,
		.. ModEntry.Instance.Helper.ModRegistry.LoadedMods.Values.SelectMany(mod => ModEntry.Instance.Helper.ModRegistry.GetModHelper(mod).Content.Cards.RegisteredTraits.Values),
	]);

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
				
				// add traits tab
				.Find(ILMatches.Ldstr("Enemies").ExtractLabels(out var labels))
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Editor_ImGuiLayout_Transpiler_HandleTraitsTab)))
				])
				
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

	private static void Editor_ImGuiLayout_Transpiler_HandleTraitsTab(G g)
	{
		if (g.state.IsOutsideRun())
			return;

		if (!ImGui.BeginTabItem("Traits"))
		{
			LastHighlightedCardId = null;
			return;
		}
		
		ImGui.SetNextWindowSizeConstraints(new Vector2(250, 400), new Vector2(5000, 5000));

		var items = AllTraits.Value
			.Select(e =>
			{
				try
				{
					return (Entry: e, LocalizedName: e.Configuration.Name?.Invoke(DB.currentLocale.locale));
				}
				catch
				{
					return (Entry: e, LocalizedName: null);
				}
			})
			.Where(e => e.Entry.UniqueName.Contains(TraitSearchText, StringComparison.InvariantCultureIgnoreCase) || e.LocalizedName?.Contains(TraitSearchText, StringComparison.InvariantCultureIgnoreCase) == true)
			.Select(e => (Entry: e.Entry, DisplayName: string.IsNullOrEmpty(e.LocalizedName) ? e.Entry.UniqueName : $"{e.LocalizedName} ({e.Entry.UniqueName})"))
			.OrderBy(e => e.DisplayName)
			.ToList();
		
		ImGui.BeginGroup();
		ImGui.Text("Left Click: add");
		ImGui.Text("Right Click: remove");
		ImGui.Text("Middle Click: reset");
		ImGui.Text("Hold Shift: permanent");
		ImGui.EndGroup();
		
		if (ImGui.InputText("Search", ref TraitSearchText, 255, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
		{
			if (items.Count != 0)
			{
				var trait = items.ElementAt(TraitCursorPosition).Entry;
				SelectNode(trait, true);
			}
		}

		var highlightedCardId = g.state.route is Combat { routeOverride: null }
			? g.boxes.FirstOrDefault(b => b.key is not null && b.key.Value.k == StableUK.card && b.IsHover())?.key?.v ?? LastHighlightedCardId
			: null;
		var highlightedCard = highlightedCardId is null ? null : g.state.FindCard(highlightedCardId.Value);
		LastHighlightedCardId = highlightedCardId;
		
		if (highlightedCard is not null && g.state.route is Combat combat2)
			combat2.hilightedCards.Add(highlightedCard.uuid);

		var size = ImGui.GetContentRegionAvail();
		size.Y -= 40;
		
		ImGui.PushItemWidth(-1);
		if (ImGui.BeginListBox("## Traits"))
		{
			for (var i = 0; i < items.Count; i++) {
				var item = items[i];
				ImGui.Selectable(item.DisplayName, i == TraitCursorPosition);
				if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) {
					TraitCursorPosition = i;
					if (highlightedCard is null)
						SelectNode(items.ElementAt(i).Entry, true);
					else
						SelectNodeForCard(highlightedCard, items.ElementAt(i).Entry, true, null);
				}
				if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
					TraitCursorPosition = i;
					if (highlightedCard is null)
						SelectNode(items.ElementAt(i).Entry, false);
					else
						SelectNodeForCard(highlightedCard, items.ElementAt(i).Entry, false, null);
				}
				if (ImGui.IsItemClicked(ImGuiMouseButton.Middle)) {
					TraitCursorPosition = i;
					if (highlightedCard is null)
						SelectNode(items.ElementAt(i).Entry, null);
					else
						SelectNodeForCard(highlightedCard, items.ElementAt(i).Entry, null, null);
				}
			}
			ImGui.EndListBox();
		}
		
		ImGui.BeginGroup();
		ImGui.Text("Highlighted card overrides (Right Click to remove)");
		ImGui.PushItemWidth(-1);
		if (ImGui.BeginListBox("## Highlighted card overrides"))
		{
			if (highlightedCard is not null)
			{
				foreach (var item in items)
				{
					var cardTraitState = ModEntry.Instance.Helper.Content.Cards.GetCardTraitState(g.state, highlightedCard, item.Entry);
					if (cardTraitState.TemporaryOverride is { } temporaryOverride)
					{
						ImGui.Selectable($"{(temporaryOverride ? "+" : "-")} temporary {item.DisplayName}");
						if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
							SelectNodeForCard(highlightedCard, item.Entry, null, false);
					}
					if (cardTraitState.PermanentOverride is { } permanentOverride)
					{
						ImGui.Selectable($"{(permanentOverride ? "+" : "-")} permanent {item.DisplayName}");
						if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
							SelectNodeForCard(highlightedCard, item.Entry, null, true);
					}
				}
			}
			ImGui.EndListBox();
		}
		ImGui.PopItemWidth();
		ImGui.EndGroup();
		
		ImGui.EndTabItem();

		void SelectNodeForCard(Card card, ICardTraitEntry trait, bool? overrideValue, bool? permanent)
			=> ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(g.state, card, trait, overrideValue, permanent ?? (Input.shift || ImGui.IsKeyDown(ImGuiKey.LeftShift)));

		void SelectNode(ICardTraitEntry trait, bool? overrideValue)
			=> g.state.GetCurrentQueue().QueueImmediate(
				new ACardSelect
				{
					browseSource = CardBrowse.Source.Deck,
					browseAction = new OverrideTraitAction { TraitUniqueName = trait.UniqueName, Permanent = Input.shift || ImGui.IsKeyDown(ImGuiKey.LeftShift), OverrideValue = overrideValue },
					allowCancel = true,
				}
			);
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
	
	private sealed class OverrideTraitAction : CardAction
	{
		public required string TraitUniqueName;
		public required bool? OverrideValue;
		public required bool Permanent;

		public override void Begin(G g, State s, Combat c)
		{
			this.timer = 0;
			if (this.selectedCard is null)
				return;
			if (ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(this.TraitUniqueName) is not { } trait)
				return;
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, this.selectedCard, trait, this.OverrideValue, permanent: this.Permanent);
		}
	}
}
