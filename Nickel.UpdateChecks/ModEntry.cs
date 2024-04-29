using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nickel.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Nickel.UpdateChecks;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;

	internal readonly Dictionary<string, IUpdateSource> UpdateSources = [];
	internal readonly Dictionary<IModManifest, (SemanticVersion Version, string UpdateInfo)?> UpdatesAvailable = [];
	internal readonly ConcurrentQueue<Action> ToRunInGameLoop = [];
	internal readonly List<Action> AwaitingUpdateInfo = [];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;

		var harmony = new Harmony(package.Manifest.UniqueName);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(G)}.{nameof(G.Render)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(G_Render_Postfix))
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
			if (!mod.ExtensionData.TryGetValue("UpdateChecks", out var rawUpdateChecks))
			{
				this.UpdatesAvailable[mod] = null;
				logger.LogDebug("Cannot check updates for mod {ModName}: `UpdateChecks` structure not defined.", mod.GetDisplayName(@long: false));
				continue;
			}

			var settings = new JsonSerializerSettings
			{
				ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
			};
			if (JsonConvert.DeserializeObject<Dictionary<string, JObject>>(JsonConvert.SerializeObject(rawUpdateChecks, settings), settings) is not { } updateChecks)
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
						return (Mod: m, Version: ((SemanticVersion Version, string UpdateInfo)?)null);

					var maxVersion = versions.Select(e => e.Version).Max();
					var maxUpdateInfos = versions
						.Where(e => e.Version == maxVersion)
						.Select(e => e.UpdateInfo);
					return (Mod: m, Version: (maxVersion, string.Join(" | ", maxUpdateInfos)));
				})
				.Where(e => e.Version is not null)
				.ToDictionary(e => e.Mod, e => e.Version!.Value);

			this.ToRunInGameLoop.Enqueue(() => this.ReportUpdates(modToVersion));
		});

	private void ReportUpdates(Dictionary<IModManifest, (SemanticVersion Version, string UpdateInfo)> updates)
	{
		foreach (var (mod, result) in updates)
		{
			this.UpdatesAvailable[mod] = result;
			if (mod.Version >= result.Version)
				continue;
			this.Logger.LogWarning("Mod {ModName} has an update {Version} available: {UpdateInfo}", mod.GetDisplayName(@long: false), result.Version, result.UpdateInfo);
		}

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
}
