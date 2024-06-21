using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Nickel.UpdateChecks;

public sealed class ModEntry : SimpleMod
{
	private static readonly Dictionary<string, Dictionary<string, string>> HardcodedUpdateCheckData = new()
	{
		{ "APurpleApple.FutureVision", new() { { "NexusMods", """{"ID": 13}""" }, { "GitHub", """{"Repository": "APurpleApple/CC_FutureVision"}""" } } },
		{ "APurpleApple.GenericArtifacts", new() { { "NexusMods", """{"ID": 16}""" }, { "GitHub", """{"Repository": "APurpleApple/APurpleApple_Artifacts"}""" } } },
		{ "APurpleApple.Shipyard", new() { { "NexusMods", """{"ID": 6}""" }, { "GitHub", """{"Repository": "APurpleApple/APurpleApple_Shipyard"}""" } } },
		{ "Arin.Randall", new() { { "GitHub", """{"Repository": "UnicornArin/CobaltCoreRandall"}""" } } },
		{ "Dave", new() { { "GitHub", """{"Repository": "rft50/cc-dave", "ReleaseTagRegex": "^dave\\-(.*)"}""" } } },
		{ "Mezz.TwosCompany", new() { { "GitHub", """{"Repository": "Mezzelo/TwosCompany"}""" } } },
		{ "rft.Jester", new() { { "GitHub", """{"Repository": "rft50/cc-dave", "ReleaseTagRegex": "^jester\\-(.*)"}""" } } },
		{ "Shockah.BetterRunSummaries", new() { { "NexusMods", """{"ID": 19}""" } } },
		{ "Shockah.Dracula", new() { { "NexusMods", """{"ID": 12}""" } } },
		{ "Shockah.DuoArtifacts", new() { { "NexusMods", """{"ID": 14}""" } } },
		{ "Shockah.Dyna", new() { { "NexusMods", """{"ID": 18}""" } } },
		{ "Shockah.Johnson", new() { { "NexusMods", """{"ID": 10}""" } } },
		{ "Shockah.Kokoro", new() { { "NexusMods", """{"ID": 4}""" } } },
		{ "Shockah.Rerolls", new() { { "NexusMods", """{"ID": 2}""" } } },
		{ "Shockah.Soggins", new() { { "NexusMods", """{"ID": 5}""" } } },
		{ "SoggoruWaffle.Tucker", new() { { "GitHub", """{"Repository": "CupOfJim/Tucker-Mod"}""" } } },
		{ "Sorwest.LenMod", new() { { "NexusMods", """{"ID": 8}""" }, { "GitHub", """{"Repository": "Sorwest/LenMod"}""" } } },
		{ "TheJazMaster.Bucket", new() { { "NexusMods", """{"ID": 9}""" }, { "GitHub", """{"Repository": "TheJazMaster/Bucket"}""" } } },
		{ "TheJazMaster.Eddie", new() { { "NexusMods", """{"ID": 17}""" }, { "GitHub", """{"Repository": "TheJazMaster/Eddie"}""" } } },
		{ "TheJazMaster.MoreDifficulties", new() { { "NexusMods", """{"ID": 15}""" }, { "GitHub", """{"Repository": "TheJazMaster/MoreDifficulties"}""" } } },
		{ "TheJazMaster.TyAndSasha", new() { { "NexusMods", """{"ID": 7}""" }, { "GitHub", """{"Repository": "TheJazMaster/TyAndSasha"}""" } } },
	};

	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	private static ISpriteEntry UpdateAvailableOverlayIcon = null!;
	private static ISpriteEntry UpdateAvailableTooltipIcon = null!;

	internal readonly Dictionary<string, IUpdateSource> UpdateSources = [];
	internal readonly Dictionary<IModManifest, UpdateDescriptor?> UpdatesAvailable = [];
	internal readonly ConcurrentQueue<Action> ToRunInGameLoop = [];
	internal readonly List<Action> AwaitingUpdateInfo = [];

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

		UpdateAvailableOverlayIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/UpdateAvailableOverlayIcon.png"));
		UpdateAvailableTooltipIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/UpdateAvailableTooltipIcon.png"));

		var harmony = new Harmony(package.Manifest.UniqueName);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(G)}.{nameof(G.Render)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(G_Render_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CornerMenu), nameof(CornerMenu.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(CornerMenu)}.{nameof(CornerMenu.Render)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CornerMenu_Render_Transpiler))
		);

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;
			this.ParseManifests(helper, logger);
		};
	}

	public override object? GetApi(IModManifest requestingMod)
		=> new ApiImplementation();

	private void ParseManifests(IModHelper helper, ILogger logger)
	{
		Dictionary<IUpdateSource, List<(IModManifest Mod, object? ManifestEntry)>> updateSourceToMod = [];

		foreach (var mod in helper.ModRegistry.LoadedMods.Values)
		{
			Dictionary<string, JObject>? updateChecks;

			if (mod.ExtensionData.TryGetValue("UpdateChecks", out var rawUpdateChecks))
			{
				var settings = new JsonSerializerSettings
				{
					ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
				};

				updateChecks = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(JsonConvert.SerializeObject(rawUpdateChecks, settings), settings);
			}
			else if (HardcodedUpdateCheckData.TryGetValue(mod.UniqueName, out var hardcodedUpdateCheckData))
			{
				logger.LogDebug("Checking hardcoded update info for mod {ModName}: `UpdateChecks` structure not defined.", mod.GetDisplayName(@long: false));
				updateChecks = hardcodedUpdateCheckData.ToDictionary(kvp => kvp.Key, kvp => JObject.Parse(kvp.Value));
			}
			else
			{
				this.UpdatesAvailable[mod] = null;
				logger.LogDebug("Cannot check updates for mod {ModName}: `UpdateChecks` structure not defined.", mod.GetDisplayName(@long: false));
				continue;
			}

			if (updateChecks is null)
			{
				this.UpdatesAvailable[mod] = null;
				logger.LogError("Cannot check updates for mod {ModName}: invalid `UpdateChecks` structure.", mod.GetDisplayName(@long: false));
				continue;
			}
			if (updateChecks.Count == 0)
			{
				this.UpdatesAvailable[mod] = null;
				continue;
			}

			var hasValidSources = false;
			foreach (var (sourceName, rawSourceManifestEntry) in updateChecks)
			{
				if (!this.UpdateSources.TryGetValue(sourceName, out var source))
					continue;
				if (!source.TryParseManifestEntry(mod, rawSourceManifestEntry, out var sourceManifestEntry))
					continue;

				hasValidSources = true;

				if (!updateSourceToMod.TryGetValue(source, out var allSourceMods))
				{
					allSourceMods = [];
					updateSourceToMod[source] = allSourceMods;
				}
				allSourceMods.Add((mod, sourceManifestEntry));
			}

			if (!hasValidSources)
			{
				this.UpdatesAvailable[mod] = null;
				logger.LogDebug("Cannot check updates for mod {ModName}: `UpdateChecks` structure is defined, but there are no installed compatible update sources.", mod.GetDisplayName(@long: false));
				continue;
			}
		}

		this.CheckUpdates(updateSourceToMod);
	}

	private void CheckUpdates(Dictionary<IUpdateSource, List<(IModManifest Mod, object? ManifestEntry)>> updateSourceToMod)
		=> Task.Run(async () =>
		{
			var updateSourceToModVersion = (await Task.WhenAll(updateSourceToMod.Select(kvp => Task.Run(async () => (Source: kvp.Key, Versions: await kvp.Key.GetLatestVersionsAsync(kvp.Value)))))).ToDictionary();

			var allMods = updateSourceToMod
				.SelectMany(kvp => kvp.Value)
				.Select(e => e.Mod)
				.ToHashSet();

			var modToVersion = allMods
				.Select(m =>
				{
					var versions = updateSourceToModVersion
						.SelectMany(kvp => kvp.Value)
						.Where(kvp => kvp.Key == m)
						.Select(kvp => kvp.Value)
						.ToList();

					if (versions.Count == 0)
						return (Mod: m, Descriptor: (UpdateDescriptor?)null);

					var maxVersion = versions.Select(e => e.Version).Max();
					var maxVersionUrls = versions
						.Where(e => e.Version == maxVersion)
						.SelectMany(e => e.Urls);
					return (Mod: m, Descriptor: new UpdateDescriptor(maxVersion, maxVersionUrls.ToList()));
				})
				.Where(e => e.Descriptor is not null)
				.ToDictionary(e => e.Mod, e => e.Descriptor!.Value);

			this.ToRunInGameLoop.Enqueue(() => this.ReportUpdates(modToVersion));
		});

	private void ReportUpdates(Dictionary<IModManifest, UpdateDescriptor> updates)
	{
		var hasOutdatedMods = false;
		foreach (var (mod, result) in updates)
		{
			this.UpdatesAvailable[mod] = result;
			if (mod.Version >= result.Version)
				continue;
			if (result.Urls.Count == 0)
				continue;
			this.Logger.LogWarning("Mod {ModName} has an update {Version} available:\n{Urls}", mod.GetDisplayName(@long: false), result.Version, string.Join("\n", result.Urls.Select(url => $"\t{url}")));
			hasOutdatedMods = true;
		}

		if (!hasOutdatedMods)
			this.Logger.LogInformation("All mods up to date.");

		var callbacks = this.AwaitingUpdateInfo.ToList();
		callbacks.Clear();
		foreach (var callback in callbacks)
			callback();
	}

	private static void G_Render_Postfix()
	{
		while (Instance.ToRunInGameLoop.TryDequeue(out var action))
			action();
	}

	private static IEnumerable<CodeInstruction> CornerMenu_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.LdcI4((int)StableSpr.buttons_menu))
				.Find(ILMatches.Call("Sprite"))
				.Replace(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CornerMenu_Render_Transpiler_HijackDraw))))
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void CornerMenu_Render_Transpiler_HijackDraw(Spr? id, double x, double y, bool flipX, bool flipY, double rotation, Vec? originPx, Vec? originRel, Vec? scale, Rect? pixelRect, Color? color, BlendState? blend, SamplerState? samplerState, Effect? effect)
	{
		Draw.Sprite(id, x, y, flipX, flipY, rotation, originPx, originRel, scale, pixelRect, color, blend, samplerState, effect);

		var updatesAvailable = Instance.UpdatesAvailable
			.Where(kvp => kvp.Value is not null)
			.Select(kvp => new KeyValuePair<IModManifest, UpdateDescriptor>(kvp.Key, kvp.Value!.Value))
			.Where(kvp => kvp.Value.Version > kvp.Key.Version)
			.ToList();
		if (updatesAvailable.Count == 0)
			return;

		Draw.Sprite(UpdateAvailableOverlayIcon.Sprite, x, y);

		if (MG.inst.g.boxes.FirstOrDefault(b => b.key is { } key && key.k == StableUK.corner_mainmenu) is not { } box)
			return;
		if (!box.IsHover())
			return;

		MG.inst.g.tooltips.Add(box.rect.xy + new Vec(15, 15), new TTDivider());
		MG.inst.g.tooltips.Add(box.rect.xy + new Vec(15, 15), new GlossaryTooltip($"ui.{Instance.Package.Manifest.UniqueName}::UpdatesAvailable")
		{
			Icon = UpdateAvailableTooltipIcon.Sprite,
			TitleColor = Colors.textBold,
			Title = Instance.Localizations.Localize(["updatesAvailableTooltip", "name"]),
			Description = string.Join("\n", updatesAvailable.Select(kvp => $"<c=textFaint>{kvp.Key.GetDisplayName(@long: false)}</c> -> <c=boldPink>{kvp.Value.Version}</c>"))
		});
	}
}
