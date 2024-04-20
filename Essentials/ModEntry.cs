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
	internal ApiImplementation Api { get; private set; }
	internal IMoreDifficultiesApi? MoreDifficultiesApi { get; private set; }

	private readonly Dictionary<Deck, Type?> ExeCache = [];
	private readonly Dictionary<Type, Deck> ExeTypeToDeck = [];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		this.Api = new();
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(
				new JsonLocalizationProvider(
					tokenExtractor: new SimpleLocalizationTokenExtractor(),
					localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
				)
			)
		);

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;
			this.MoreDifficultiesApi = helper.ModRegistry.GetApi<IMoreDifficultiesApi>("TheJazMaster.MoreDifficulties", new(1, 4, 1));
		};

		var harmony = new Harmony(package.Manifest.UniqueName);
		CardCodexFiltering.ApplyPatches(harmony);
		CrewSelection.ApplyPatches(harmony);
		ExeBlacklist.ApplyPatches(harmony);
		FixIsaacUnlock.ApplyPatches(harmony);
		MemorySelection.ApplyPatches(harmony);
		ModDescriptions.ApplyPatches(harmony);
		StarterDeckPreview.ApplyPatches(harmony);
	}

	public override object? GetApi(IModManifest requestingMod)
		=> new ApiImplementation();

	internal Type? GetExeType(Deck deck)
	{
		if (this.ExeCache.TryGetValue(deck, out var exeType))
			return exeType;

		if (this.Helper.Content.Characters.LookupByDeck(deck) is { } entry && entry.Configuration.ExeCardType is { } entryExeType)
		{
			this.ExeCache[deck] = entryExeType;
			this.ExeTypeToDeck[entryExeType] = deck;
			return entryExeType;
		}

		var fakeShip = Mutil.DeepCopy(StarterShip.ships.Values.First());
		fakeShip.cards.Clear();
		fakeShip.artifacts.Clear();

		var oldDemo = FeatureFlags.Demo;
		try
		{
			FeatureFlags.Demo = DemoMode.PAX;
			var fakeState = Mutil.DeepCopy(DB.fakeState);
			fakeState.slot = null;
			fakeState.PopulateRun(
				shipTemplate: fakeShip,
				chars: NewRunOptions.allChars
					.Where(d => d != deck && d != Deck.dizzy) // need at least 2 characters total, otherwise it will always throw
					.ToHashSet()
			);

			exeType = fakeState.deck
				.Where(card => card is not ColorlessDizzySummon)
				.SingleOrDefault(card => card.GetMeta().deck == Deck.colorless && card.GetFullDisplayName().Contains(".EXE", StringComparison.OrdinalIgnoreCase))?.GetType();
			this.ExeCache[deck] = exeType;
			if (exeType is not null)
				this.ExeTypeToDeck[exeType] = deck;
			return exeType;
		}
		catch
		{
			this.ExeCache[deck] = null;
			return null;
		}
		finally
		{
			FeatureFlags.Demo = oldDemo;
		}
	}
}
