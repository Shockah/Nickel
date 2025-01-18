using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nickel.Essentials;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public bool SortModdedCrewByName;
	
	[JsonProperty]
	public bool SortVanillaCrewByName;
}

internal static class CrewAndShipSorting
{
	private static List<Deck>? OriginalCharacterOrder;
	
	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.OnEnter)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_OnEnter_Postfix))
		);

	public static IModSettingsApi.IModSetting MakeSettings(IModSettingsApi api)
		=> api.MakeList([
			api.MakeCheckbox(
				title: () => ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortCrewByName", "name"]),
				getter: () => ModEntry.Instance.Settings.ProfileBased.Current.SortModdedCrewByName,
				setter: (_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.SortModdedCrewByName = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}::CrewAndShipSorting::{nameof(ProfileSettings.SortModdedCrewByName)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortCrewByName", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortCrewByName", "description"])
				}
			]),
			api.MakeConditional(
				api.MakeCheckbox(
					title: () => ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortVanillaCrewByName", "name"]),
					getter: () => ModEntry.Instance.Settings.ProfileBased.Current.SortVanillaCrewByName,
					setter: (_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.SortVanillaCrewByName = value
				).SetTooltips(() => [
					new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}::CrewAndShipSorting::{nameof(ProfileSettings.SortVanillaCrewByName)}")
					{
						TitleColor = Colors.textBold,
						Title = ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortVanillaCrewByName", "name"]).Trim(),
						Description = ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortVanillaCrewByName", "description"])
					}
				]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.SortModdedCrewByName
			)
		]);

	internal static void Refresh()
	{
		OriginalCharacterOrder ??= NewRunOptions.allChars.ToList();
		NewRunOptions.allChars.Clear();

		IEnumerable<Deck> sortedCharacters;
		
		if (ModEntry.Instance.Settings.ProfileBased.Current.SortModdedCrewByName)
		{
			if (ModEntry.Instance.Settings.ProfileBased.Current.SortVanillaCrewByName)
			{
				sortedCharacters = OriginalCharacterOrder
					.Select(deck => (Deck: deck, DeckEntry: ModEntry.Instance.Helper.Content.Decks.LookupByDeck(deck)))
					.Where(e => e.DeckEntry is not null)
					.OrderBy(e => e.DeckEntry?.Configuration.Name?.Invoke(DB.currentLocale.locale))
					.ThenBy(e => e.Deck)
					.Select(e => e.Deck);
			}
			else
			{
				sortedCharacters = [
					.. OriginalCharacterOrder
						.Select(deck => (Deck: deck, DeckEntry: ModEntry.Instance.Helper.Content.Decks.LookupByDeck(deck)))
						.Where(e => e.DeckEntry?.ModOwner == ModEntry.Instance.Helper.ModRegistry.VanillaModManifest)
						.OrderBy(e => e.Deck == Deck.colorless ? int.MaxValue : (int)e.Deck)
						.Select(e => e.Deck),
					.. OriginalCharacterOrder
						.Select(deck => (Deck: deck, DeckEntry: ModEntry.Instance.Helper.Content.Decks.LookupByDeck(deck)))
						.Where(e => e.DeckEntry?.ModOwner != ModEntry.Instance.Helper.ModRegistry.VanillaModManifest)
						.Where(e => e.DeckEntry is not null)
						.OrderBy(e => e.DeckEntry?.Configuration.Name?.Invoke(DB.currentLocale.locale))
						.ThenBy(e => e.Deck)
						.Select(e => e.Deck)
				];
			}
		}
		else
		{
			sortedCharacters = OriginalCharacterOrder;
		}
		
		NewRunOptions.allChars.AddRange(sortedCharacters);
	}

	private static void NewRunOptions_OnEnter_Postfix()
		=> Refresh();
}
