using FSPRO;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nickel.Essentials;

internal static class ShipSelection
{
	private const int MaxShipsOnScreen = 8;
	internal static readonly UK ShipSelectionToggleUiKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	internal static readonly UK ShipSelectionUiKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

	private static ISpriteEntry ShipButtonSprite = null!;
	private static ISpriteEntry ShipButtonOnSprite = null!;
	private static ISpriteEntry ShipsButtonSprite = null!;
	private static ISpriteEntry ShipsButtonOnSprite = null!;

	private static int ScrollPosition;
	internal static bool ShowingShips { get; private set; }
	internal static StarterShip? PreviewingShip { get; private set; }

	private static int MaxScroll
		=> Math.Max(0, StarterShip.ships.Count - MaxShipsOnScreen);

	private static int MaxPageByPageScroll
	{
		get
		{
			var totalPages = (int)Math.Ceiling((double)StarterShip.ships.Count / MaxShipsOnScreen);
			var maxScroll = Math.Max(0, (totalPages - 1) * MaxShipsOnScreen);
			return maxScroll;
		}
	}

	public static void ApplyPatches(IHarmony harmony)
	{
		ShipButtonSprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/ShipButton.png"));
		ShipButtonOnSprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/ShipButtonOn.png"));
		ShipsButtonSprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/ShipsButton.png"));
		ShipsButtonOnSprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/ShipsButtonOn.png"));
		
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.OnEnter)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_OnEnter_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_Render_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.CharSelect)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_CharSelect_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.RenderWarning)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_RenderWarning_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RunConfig), nameof(RunConfig.GetSelectionState)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunConfig_GetSelectionState_Postfix))
		);
	}

	public static IModSettingsApi.IModSetting MakeSettings(IModSettingsApi api)
		=> api.MakeCheckbox(
			title: () => ModEntry.Instance.Localizations.Localize(["crewSelection", "detailedCrewInfoSetting", "name"]),
			getter: () => ModEntry.Instance.Settings.ProfileBased.Current.DetailedCrewInfo,
			setter: (_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.DetailedCrewInfo = value
		).SetTooltips(() => [
			new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}::DetailedCrewInfoSetting")
			{
				TitleColor = Colors.textBold,
				Title = ModEntry.Instance.Localizations.Localize(["crewSelection", "detailedCrewInfoSetting", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["crewSelection", "detailedCrewInfoSetting", "description"])
			}
		]);

	private static void NewRunOptions_OnEnter_Postfix()
	{
		// reset the scroll position to the very top
		ScrollPosition = 0;
		ShowingShips = false;
	}

	private static void NewRunOptions_Render_Prefix()
	{
		if (!ShowingShips)
			return;
		
		// handling mouse scroll wheel to page the character list
		var mouseScroll = (int)Math.Round(-Input.scrollY / 120);
		if (mouseScroll != 0)
			ScrollPosition = Math.Clamp(ScrollPosition + mouseScroll, 0, MaxScroll);
	}

	private static bool NewRunOptions_CharSelect_Prefix(G g)
	{
		if (!ShowingShips)
			return true;

		const int leftX = 0;
		const int topY = 76;
		const int spacing = 18;

		PreviewingShip = null;
		for (var i = 0; i < MaxShipsOnScreen; i++)
		{
			if (i + ScrollPosition >= StarterShip.ships.Count)
				break;
			var (shipKey, ship) = StarterShip.ships.Skip(i + ScrollPosition).First();

			var buttonBaseY = topY + i * spacing;
			
			var shipButtonResult = SharedArt.ButtonText(
				g, Vec.Zero, new UIKey(ShipSelectionUiKey, str: shipKey), "", rect: new(leftX, buttonBaseY, 86, 18),
				onMouseDown: new MouseDownHandler(() =>
				{
					Audio.Play(Event.Click);
					g.state.runConfig.selectedShip = shipKey;
					ShowingShips = false;
				}),
				showAsPressed: g.state.runConfig.selectedShip == shipKey,
				sprite: ShipButtonSprite.Sprite, spriteDown: ShipButtonOnSprite.Sprite, spriteHover: ShipButtonOnSprite.Sprite
			);

			var firstArtifact = ship.artifacts.MinBy(a => a is ShieldPrep) ?? new ShieldPrep();
			var artifactSprite = DB.artifactSprites.TryGetValue(firstArtifact.Key(), out var tryArtifactSprite) ? tryArtifactSprite : StableSpr.artifacts_ShieldPrep;
			Draw.Sprite(artifactSprite, leftX + 4, buttonBaseY + 2 + (shipButtonResult.isHover ? 1 : 0));
			Draw.Text(Loc.T($"ship.{shipKey}.name"), leftX + 20, buttonBaseY + 6 + (shipButtonResult.isHover ? 1 : 0), color: shipButtonResult.isHover ? Colors.textChoiceHoverActive : Colors.textMain);

			if (shipButtonResult.isHover && g.state.runConfig.selectedShip != shipKey)
				PreviewingShip = StarterShip.ships[shipKey];
		}
		
		// rendering ship list scrolling arrow buttons
		if (ScrollPosition > 0)
		{
			Rect rect = new(NewRunOptions.charSelectPos.x + 19, NewRunOptions.charSelectPos.y - 53, 31, 26);
			OnMouseDown onMouseDown = new MouseDownHandler(() => ScrollPosition = Math.Max(0, ScrollPosition - MaxShipsOnScreen));
			SharedArt.ButtonSprite(g, rect, StableUK.btn_move_left, ModEntry.Instance.ScrollUpSprite.Sprite, ModEntry.Instance.ScrollUpOnSprite.Sprite, onMouseDown: onMouseDown);
		}
		if (ScrollPosition < MaxScroll)
		{
			Rect rect = new(NewRunOptions.charSelectPos.x + 19, NewRunOptions.charSelectPos.y + 140, 31, 26);
			OnMouseDown onMouseDown = new MouseDownHandler(() => ScrollPosition = Math.Clamp(ScrollPosition + MaxShipsOnScreen, 0, MaxPageByPageScroll));
			SharedArt.ButtonSprite(g, rect, StableUK.btn_move_right, ModEntry.Instance.ScrollDownSprite.Sprite, ModEntry.Instance.ScrollDownOnSprite.Sprite, onMouseDown: onMouseDown);
		}

		return false;
	}

	private static void NewRunOptions_RenderWarning_Postfix(G g)
		=> SharedArt.ButtonText(
			g, Vec.Zero, new UIKey(ShipSelectionToggleUiKey), "",
			onMouseDown: new MouseDownHandler(() =>
			{
				Audio.Play(Event.Click);
				ShowingShips = !ShowingShips;
			}),
			rect: new(NewRunOptions.shipSelectPos.x - 49, NewRunOptions.shipSelectPos.y + 54, 100, 25),
			showAsPressed: ShowingShips,
			sprite: ShipsButtonSprite.Sprite, spriteDown: ShipsButtonOnSprite.Sprite, spriteHover: ShipsButtonOnSprite.Sprite
		);

	private static void RunConfig_GetSelectionState_Postfix(ref (List<KeyValuePair<string, StarterShip>>, StarterShip, int) __result)
	{
		if (!ShowingShips)
			return;
		if (PreviewingShip is not null)
			__result = (__result.Item1, PreviewingShip, StarterShip.ships.Values.Select((ship, index) => (Ship: ship, Index: index)).First(e => e.Ship == PreviewingShip).Index);
	}
}
