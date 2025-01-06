using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel.Essentials;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;
	internal readonly ApiImplementation Api;
	internal readonly Settings Settings;
	internal IMoreDifficultiesApi? MoreDifficultiesApi { get; private set; }

	internal static bool StopStateTransitions;

	internal readonly ISpriteEntry ScrollUpSprite;
	internal readonly ISpriteEntry ScrollUpOnSprite;
	internal readonly ISpriteEntry ScrollDownSprite;
	internal readonly ISpriteEntry ScrollDownOnSprite;

	internal readonly HookManager<IEssentialsApi.IHook> Hooks;

	private readonly Dictionary<Deck, Type?> ExeCache = [];
	private readonly Dictionary<Type, Deck> ExeTypeToDeck = [];

	private IWritableFileInfo SettingsFile
		=> this.Helper.Storage.GetMainStorageFile("json");

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		this.Hooks = new(package.Manifest.UniqueName);
		this.Api = new();
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(
				new JsonLocalizationProvider(
					tokenExtractor: new SimpleLocalizationTokenExtractor(),
					localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
				)
			)
		);
		this.Settings = helper.Storage.LoadJson<Settings>(this.SettingsFile);
		
		this.ScrollUpSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/ScrollUp.png"));
		this.ScrollUpOnSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/ScrollUpOn.png"));
		this.ScrollDownSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/ScrollDown.png"));
		this.ScrollDownOnSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/ScrollDownOn.png"));
		
		helper.ModRegistry.AwaitApi<IMoreDifficultiesApi>(
			"TheJazMaster.MoreDifficulties", new(1, 4, 1),
			api => this.MoreDifficultiesApi = api
		);
		helper.ModRegistry.AwaitApi<IModSettingsApi>(
			"Nickel.ModSettings",
			api => api.RegisterModSettings(
				api.MakeList([
					api.MakeProfileSelector(
						() => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
						this.Settings.ProfileBased
					),
					ArtifactCodexCategories.MakeSettings(api),
					CrewSelection.MakeSettings(api),
					StarterDeckPreview.MakeSettings(api),
					ExeBlacklist.MakeSettings(api),
					CardBrowseCurrentPile.MakeSettings(api),
					ModDescriptions.MakeSettings(api),
				]).SubscribeToOnMenuClose(
					_ => helper.Storage.SaveJson(this.SettingsFile, this.Settings)
				)
			)
		);

		var harmony = helper.Utilities.Harmony;
		ArtifactCodexCategories.ApplyPatches(harmony);
		CardBrowseCurrentPile.ApplyPatches(harmony);
		CardBrowseOrderSortMode.ApplyPatches(harmony);
		CardCodexFiltering.ApplyPatches(harmony);
		CrewSelection.ApplyPatches(harmony);
		ExeBlacklist.ApplyPatches(harmony);
		LogbookReplacement.ApplyPatches(harmony);
		MemorySelection.ApplyPatches(harmony);
		ModDescriptions.ApplyPatches(harmony);
		SaveImport.ApplyPatches(harmony);
		ShipSelection.ApplyPatches(harmony);
		StarterDeckPreview.ApplyPatches(harmony);
		TooltipScrolling.ApplyPatches(harmony);
		UnlockedWindowResize.ApplyPatches(harmony);
		
		this.Hooks.Register(new CardBrowseOrderSortMode.Hook());

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.ShuffleDeck))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.ShuffleDeck)}`"),
			prefix: new HarmonyMethod(this.GetType(), nameof(State_ShuffleDeck_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.GoToZone))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.GoToZone)}`"),
			prefix: new HarmonyMethod(this.GetType(), nameof(State_GoToZone_Prefix))
		);
	}

	public override object GetApi(IModManifest requestingMod)
		=> new ApiImplementation();

	private void PrepareExeInfoIfNeeded()
	{
		if (this.ExeCache.Count != 0)
			return;

		var fakeShip = Mutil.DeepCopy(StarterShip.ships.Values.First());
		fakeShip.cards.Clear();
		fakeShip.artifacts.Clear();

		var oldDemo = FeatureFlags.Demo;
		try
		{
			FeatureFlags.Demo = DemoMode.PAX;
			StopStateTransitions = true;

			var fakeState = Mutil.DeepCopy(DB.fakeState);
			fakeState.slot = null;
			this.Helper.ModData.SetModData(fakeState, "RunningDataCollectingPopulateRun", true);

			foreach (var deck in NewRunOptions.allChars)
			{
				if (this.Helper.Content.Characters.V2.LookupByDeck(deck) is { } deckEntry)
				{
					if (deckEntry.Configuration.ExeCardType is { } entryExeType)
					{
						this.ExeCache[deck] = entryExeType;
						this.ExeTypeToDeck[entryExeType] = deck;
						continue;
					}

					if (deckEntry.ModOwner.ModType != "Nickel.Legacy")
					{
						this.ExeCache[deck] = null;
						continue;
					}
				}

				try
				{
					fakeState.PopulateRun(
						shipTemplate: fakeShip,
						newMap: new MapDemo(),
						chars: NewRunOptions.allChars
							.Where(d => d != deck && d != Deck.dizzy) // need at least 2 characters total, otherwise it will always throw
							.ToHashSet(),
						giveRunStartRewards: true
					);

					var exeType = fakeState.deck
						.Where(card => card is not ColorlessDizzySummon)
						.SingleOrDefault(card => card.GetMeta().deck == Deck.colorless && card.GetFullDisplayName().Contains(".EXE", StringComparison.OrdinalIgnoreCase))?.GetType();
					this.ExeCache[deck] = exeType;
					if (exeType is not null)
						this.ExeTypeToDeck[exeType] = deck;
				}
				catch
				{
					// ignored
				}
			}
		}
		finally
		{
			FeatureFlags.Demo = oldDemo;
			StopStateTransitions = false;
		}
	}

	internal Type? GetExeCardTypeForDeck(Deck deck)
	{
		this.PrepareExeInfoIfNeeded();
		return this.ExeCache.GetValueOrDefault(deck);
	}

	internal Deck? GetDeckForExeCardType(Type type)
	{
		this.PrepareExeInfoIfNeeded();
		return this.ExeTypeToDeck.TryGetValue(type, out var deck) ? deck : null;
	}

	private static bool State_ShuffleDeck_Prefix()
		=> !StopStateTransitions;

	private static bool State_GoToZone_Prefix(ref Route __result)
	{
		if (!StopStateTransitions)
			return true;

		__result = new Route();
		return false;
	}
}
