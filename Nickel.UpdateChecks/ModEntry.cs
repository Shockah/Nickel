using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
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

	internal readonly Dictionary<string, IUpdateSource> UpdateSourceKeyToSource = [];
	internal readonly Dictionary<IUpdateSource, string> UpdateSourceToKey = [];
	internal readonly Dictionary<IModManifest, List<UpdateDescriptor>> UpdatesAvailable = [];
	internal readonly ConcurrentQueue<Action> ToRunInGameLoop = [];
	internal readonly List<Action> AwaitingUpdateInfo = [];
	internal readonly Settings Settings;

	private (Task Task, CancellationTokenSource Token)? CurrentUpdateCheckTask;

	internal IWritableFileInfo SettingsFile
		=> this.Helper.Storage.GetMainStorageFile("json");

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		this.Settings = helper.Storage.LoadJson<Settings>(this.SettingsFile);

		helper.Events.OnGameClosing += (_, _) =>
		{
			if (this.CurrentUpdateCheckTask?.Task is { IsCompleted: false } task)
			{
				try
				{
					this.Logger.LogInformation("Update checks not done, waiting up to 15s...");
					task.Wait(TimeSpan.FromSeconds(15));
				}
				catch
				{
					// ignored
				}
			}
			
			this.ProcessActionsToRunInGameLoop();
		};
		
		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			switch (phase)
			{
				case ModLoadPhase.BeforeGameAssembly:
					this.SetupBeforeGameAssembly();
					break;
				case ModLoadPhase.AfterGameAssembly:
					this.SetupAfterGameAssembly();
					break;
				case ModLoadPhase.AfterDbInit:
					break;
			}
			
			this.ProcessActionsToRunInGameLoop();
		};
	}

	private void ProcessActionsToRunInGameLoop()
	{
		while (this.ToRunInGameLoop.TryDequeue(out var action))
			action();
	}

	private void SetupBeforeGameAssembly()
		=> this.ParseManifestsAndRequestUpdateInfo();

	private void SetupAfterGameAssembly()
	{
		var harmony = this.Helper.Utilities.Harmony;
		
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(G)}.{nameof(G.Render)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(G_Render_Postfix))
		);
	}

	public override object GetApi(IModManifest requestingMod)
		=> new ApiImplementation();

	internal static string GetModNameForUpdatePurposes(IModManifest mod)
	{
		if (mod.ExtensionData.TryGetValue("UpdateChecks", out var rawUpdateChecks))
		{
			var settings = new JsonSerializerSettings { ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor };
			var updateChecks = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(JsonConvert.SerializeObject(rawUpdateChecks, settings), settings);

			if (updateChecks is not null && updateChecks.TryGetValue("ModNameForUpdatePurposes", out var rawModNameForUpdatePurposes) && rawModNameForUpdatePurposes.Value<string>() is { } modNameForUpdatePurposes)
				return modNameForUpdatePurposes;
		}

		return mod.DisplayName ?? mod.UniqueName;
	}

	internal void ParseManifestsAndRequestUpdateInfo(IUpdateSource? sourceToUpdate = null)
	{
		Dictionary<IUpdateSource, List<(IModManifest Mod, object? ManifestEntry)>> updateSourceToMod = [];
		var settings = new JsonSerializerSettings { ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor };

		foreach (var mod in this.Helper.ModRegistry.ResolvedMods.Values)
		{
			Dictionary<string, JToken>? updateChecks;

			if (mod.ExtensionData.TryGetValue("UpdateChecks", out var rawUpdateChecks))
			{
				updateChecks = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(JsonConvert.SerializeObject(rawUpdateChecks, settings), settings);
			}
			else if (HardcodedUpdateCheckData.TryGetValue(mod.UniqueName, out var hardcodedUpdateCheckData))
			{
				this.Logger.LogDebug("Checking hardcoded update info for mod {ModName}: `UpdateChecks` structure not defined.", GetModNameForUpdatePurposes(mod));
				updateChecks = hardcodedUpdateCheckData.ToDictionary(kvp => kvp.Key, JToken (kvp) => JObject.Parse(kvp.Value));
			}
			else
			{
				this.UpdatesAvailable[mod] = [];
				this.Logger.LogDebug("Cannot check updates for mod {ModName}: `UpdateChecks` structure not defined.", GetModNameForUpdatePurposes(mod));
				continue;
			}

			if (updateChecks is null)
			{
				this.UpdatesAvailable[mod] = [];
				this.Logger.LogError("Cannot check updates for mod {ModName}: invalid `UpdateChecks` structure.", GetModNameForUpdatePurposes(mod));
				continue;
			}
			if (updateChecks.Count == 0)
			{
				this.UpdatesAvailable[mod] = [];
				continue;
			}

			var hasValidSources = false;
			foreach (var (sourceName, rawTokenSourceManifestEntry) in updateChecks)
			{
				if (rawTokenSourceManifestEntry is not JObject rawSourceManifestEntry)
					continue;
				
				if (!this.UpdateSourceKeyToSource.TryGetValue(sourceName, out var source))
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
				this.UpdatesAvailable[mod] = [];
				this.Logger.LogDebug("Cannot check updates for mod {ModName}: `UpdateChecks` structure is defined, but there are no installed compatible update sources.", GetModNameForUpdatePurposes(mod));
			}
		}

		if (sourceToUpdate is null)
			this.CheckUpdates(updateSourceToMod);
		else if (updateSourceToMod.TryGetValue(sourceToUpdate, out var entriesForSourceToUpdate))
			this.CheckUpdates(new Dictionary<IUpdateSource, List<(IModManifest Mod, object? ManifestEntry)>> { { sourceToUpdate, entriesForSourceToUpdate } });
	}

	private void CheckUpdates(Dictionary<IUpdateSource, List<(IModManifest Mod, object? ManifestEntry)>> updateSourceToMod)
	{
		this.CurrentUpdateCheckTask?.Token.Cancel();

		foreach (var availableUpdate in this.UpdatesAvailable)
		{
			availableUpdate.Value.RemoveAll(d => updateSourceToMod.Keys.Select(s => this.UpdateSourceToKey.GetValueOrDefault(s)).Contains(d.SourceKey));
			if (availableUpdate.Value.Count == 0)
				this.UpdatesAvailable.Remove(availableUpdate.Key);
		}

		var token = new CancellationTokenSource();
		this.CurrentUpdateCheckTask = (
			Task: Task.Run(async () =>
			{
				var updateSourceToModVersion = (await Task.WhenAll(updateSourceToMod.Select(kvp => Task.Run(async () => (Source: kvp.Key, Versions: await kvp.Key.GetLatestVersionsAsync(kvp.Value)), token.Token)))).ToDictionary();

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
							return (Mod: m, Descriptors: []);

						var maxVersion = versions.Select(e => e.Version).Max();

						return (
							Mod: m,
							Descriptors: versions
								.Where(e => e.Version == maxVersion)
								.ToList()
						);
					})
					.Where(e => e.Descriptors.Count != 0)
					.ToDictionary(e => e.Mod, e => e.Descriptors);

				this.ToRunInGameLoop.Enqueue(() => this.ReportUpdates(modToVersion));
			}, token.Token),
			Token: token
		);
	}

	private void ReportUpdates(Dictionary<IModManifest, List<UpdateDescriptor>> updates)
	{
		var hasOutdatedMods = false;
		foreach (var (mod, descriptors) in updates)
		{
			this.UpdatesAvailable[mod] = descriptors;
			if (descriptors.Count == 0)
				continue;

			var descriptorVersion = descriptors[0].Version;
			if (mod.Version >= descriptorVersion)
				continue;

			if (this.Settings.IgnoredUpdates.TryGetValue(mod.UniqueName, out var ignoredUpdate) && ignoredUpdate == descriptorVersion)
			{
				this.Logger.LogDebug("Mod {ModName} {OldVersion} has an update {NewVersion} available, but it's being ignored:\n{Urls}", GetModNameForUpdatePurposes(mod), mod.Version, descriptorVersion, string.Join("\n", descriptors.Select(d => $"\t{d.Url}")));
			}
			else
			{
				this.Logger.LogWarning("Mod {ModName} {OldVersion} has an update {NewVersion} available:\n{Urls}", GetModNameForUpdatePurposes(mod), mod.Version, descriptorVersion, string.Join("\n", descriptors.Select(d => $"\t{d.Url}")));
				hasOutdatedMods = true;
			}
		}

		if (!hasOutdatedMods)
			this.Logger.LogInformation("All mods up to date.");

		var callbacks = this.AwaitingUpdateInfo.ToList();
		callbacks.Clear();
		foreach (var callback in callbacks)
			callback();
	}

	private static void G_Render_Postfix()
		=> Instance.ProcessActionsToRunInGameLoop();
}
