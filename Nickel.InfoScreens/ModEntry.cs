using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nickel.InfoScreens;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;
	internal readonly MultiPool ArgsPool = new();
	internal readonly OrderedList<IInfoScreensApi.IInfoScreenEntry, double> RequestedInfoScreens = new(false);
	private InfoScreenEntry? CurrentInfoScreen;
	
	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(
				new JsonLocalizationProvider(
					tokenExtractor: new SimpleLocalizationTokenExtractor(),
					localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
				)
			)
		);
		
		helper.Utilities.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.Render))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(G)}.{nameof(G.Render)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(G_Render_Prefix))
		);
	}

	public override object GetApi(IModManifest requestingMod)
		=> new ApiImplementation(requestingMod);

	internal IInfoScreensApi.IInfoScreenEntry RequestInfoScreen(IModManifest owner, string name, Route route, double priority)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		var entry = new InfoScreenEntry(owner, uniqueName, name, route, priority);

		foreach (var existingEntry in this.RequestedInfoScreens)
		{
			if (existingEntry.UniqueName != uniqueName)
				continue;
			this.RequestedInfoScreens.Remove(existingEntry);
			break;
		}
		
		this.RequestedInfoScreens.Add(entry, priority);
		return entry;
	}

	internal void OnClose(G g, InfoScreenEntry entry)
	{
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (g.state is null)
			return;
		var replacementRoute = g.state.route is Combat combat
			? combat.routeOverride as InfoScreenReplacementRoute
			: g.state.route as InfoScreenReplacementRoute;
		if (replacementRoute is null)
			return;
		if (replacementRoute.Entry != entry)
			return;
		if (entry.State != IInfoScreensApi.IInfoScreenState.Visible)
			return;

		entry.State = IInfoScreensApi.IInfoScreenState.Finished;
		this.CurrentInfoScreen = null;
	}

	internal void Cancel(G g, IInfoScreensApi.IInfoScreenEntry genericEntry)
	{
		if (genericEntry is not InfoScreenEntry entry)
			return;

		switch (entry.State)
		{
			case IInfoScreensApi.IInfoScreenState.Requested:
				entry.State = IInfoScreensApi.IInfoScreenState.Cancelled;
				this.RequestedInfoScreens.Remove(entry);
				break;
			case IInfoScreensApi.IInfoScreenState.Visible:
				var replacementRoute = g?.state.route is Combat combat
					? combat.routeOverride as InfoScreenReplacementRoute
					: g?.state.route as InfoScreenReplacementRoute;
				if (replacementRoute is not null)
					g?.CloseRoute(replacementRoute, CBResult.Cancel);
				entry.State = IInfoScreensApi.IInfoScreenState.ForciblyCancelled;
				this.RequestedInfoScreens.Remove(entry);
				this.CurrentInfoScreen = null;
				break;
			case IInfoScreensApi.IInfoScreenState.Finished:
			case IInfoScreensApi.IInfoScreenState.Cancelled:
			case IInfoScreensApi.IInfoScreenState.ForciblyCancelled:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private static void G_Render_Prefix(G __instance)
	{
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (__instance.state is null)
			return;
		
		var replacementRoute = __instance.state.route is Combat combat
			? combat.routeOverride as InfoScreenReplacementRoute
			: __instance.state.route as InfoScreenReplacementRoute;

		if (replacementRoute is null)
		{
			if (Instance.CurrentInfoScreen is not null)
			{
				Instance.CurrentInfoScreen.State = IInfoScreensApi.IInfoScreenState.Finished;
				Instance.CurrentInfoScreen = null;
			}
			
			if (Instance.RequestedInfoScreens.FirstOrDefault() is not InfoScreenEntry infoScreen)
				return;
			Instance.RequestedInfoScreens.Remove(infoScreen);

			if (__instance.state.route is Combat combat2)
				combat2.routeOverride = new InfoScreenReplacementRoute(infoScreen, infoScreen.Route, combat2.routeOverride);
			else
				__instance.state.route = new InfoScreenReplacementRoute(infoScreen, infoScreen.Route, __instance.state.route);
			infoScreen.State = IInfoScreensApi.IInfoScreenState.Visible;
			Instance.CurrentInfoScreen = infoScreen;
		}
		else
		{
			replacementRoute.Entry.State = IInfoScreensApi.IInfoScreenState.Visible;
			Instance.CurrentInfoScreen = replacementRoute.Entry;
		}
	}
}
