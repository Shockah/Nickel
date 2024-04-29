using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Nickel.UpdateChecks.NexusMods;

public sealed class ModEntry : SimpleMod, IUpdateSource
{
	private const string NexusApiKey = "";

	internal static ModEntry Instance { get; private set; } = null!;

	private readonly JsonSerializerSettings SerializerSettings;
	private readonly JsonSerializer Serializer;
	private readonly HttpClient Client;
	private readonly SemaphoreSlim Semaphore = new(1, 1);
	private Database Database = new();

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;

		this.SerializerSettings = new()
		{
			ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
		};
		this.SerializerSettings.Converters.Add(new SemanticVersionConverter());
		this.Serializer = JsonSerializer.Create(this.SerializerSettings);

		this.Client = new();
		this.Client.DefaultRequestHeaders.Add("Application-Name", this.Package.Manifest.UniqueName);
		this.Client.DefaultRequestHeaders.Add("Application-Version", this.Package.Manifest.Version.ToString());
		this.Client.DefaultRequestHeaders.Add("apikey", NexusApiKey);

		helper.ModRegistry.GetApi<IUpdateChecksApi>("Nickel.UpdateChecks")!.RegisterUpdateSource("NexusMods", this);
	}

	public bool TryParseManifestEntry(IModManifest mod, object? rawManifestEntry, out object? manifestEntry)
	{
		manifestEntry = JsonConvert.DeserializeObject<ManifestEntry>(JsonConvert.SerializeObject(rawManifestEntry, this.SerializerSettings), this.SerializerSettings);
		if (manifestEntry is null)
			this.Logger.LogError("Cannot check NexusMods updates for mod {ModName}: invalid `UpdateChecks` structure.", mod.GetDisplayName(@long: false));
		return manifestEntry is not null;
	}

	public async Task<IReadOnlyDictionary<IModManifest, (SemanticVersion Version, string UpdateInfo)>> GetLatestVersionsAsync(IEnumerable<(IModManifest Mod, object? ManifestEntry)> mods)
	{
		await this.Semaphore.WaitAsync();
		try
		{
			var results = new Dictionary<IModManifest, (SemanticVersion Version, string UpdateInfo)>();
			var now = Stopwatch.GetTimestamp();
			var remainingMods = mods
				.Where(e => e.ManifestEntry is ManifestEntry)
				.Select(e => (Mod: e.Mod, Entry: (ManifestEntry)e.ManifestEntry!))
				.ToList();

			// updating version data from the 3 10-element lists
			// if we only have 3 mods to fetch, we skip to save on requests

			if (remainingMods.Count >= 3)
			{
				var latestAddedModsTask = this.GetLatestAddedMods();
				var latestUpdatedModsTask = this.GetLatestUpdatedMods();
				var trendingModsTask = this.GetTrendingMods();

				var latestAddedMods = await latestAddedModsTask;
				var latestUpdatedMods = await latestUpdatedModsTask;
				var trendingMods = await trendingModsTask;

				foreach (var model in latestAddedMods.Concat(latestUpdatedMods).Concat(trendingMods))
				{
					if (!SemanticVersionParser.TryParse(model.Version, out var version))
						continue;
					if (remainingMods.FirstOrNull(e => e.Entry.ID == model.ID) is not { } modEntry)
						continue;

					remainingMods.Remove(modEntry);
					results[modEntry.Mod] = (Version: version, UpdateInfo: $"https://www.nexusmods.com/cobaltcore/mods/{model.ID}");
					this.Database.ModIdToVersion[model.ID] = version;
				}

				if (remainingMods.Count == 0)
					return results;
			}

			// if we had a previous update within the last month, we can try checking if any of the remaining mods had any updates and return early if not
			// if we only have 2 mods to fetch, we skip to save on requests

			if (remainingMods.Count >= 2)
			{
				var timeSinceLastUpdate = now - this.Database.LastUpdate;
				if (timeSinceLastUpdate < 60 * 60 * 24 * 28)
				{
					var updatedMods = await this.GetUpdatedMods(timeSinceLastUpdate);

					foreach (var modEntry in remainingMods)
					{
						if (updatedMods.Any(model => model.ID == modEntry.Entry.ID))
							continue;
						if (!this.Database.ModIdToVersion.TryGetValue(modEntry.Entry.ID, out var persistedVersion))
							continue;

						remainingMods.Remove(modEntry);
						results[modEntry.Mod] = (Version: persistedVersion, UpdateInfo: $"https://www.nexusmods.com/cobaltcore/mods/{modEntry.Entry.ID}");
					}

					if (remainingMods.Count == 0)
						return results;
				}
			}

			// if we still have some remaining mods, we gotta fetch them 1-by-1

			var modDetails = await Task.WhenAll(
				remainingMods
					.Select(modEntry => Task.Run(async () =>
					{
						try
						{
							return (ModEntry: modEntry, Model: await this.GetMod(modEntry.Entry.ID));
						}
						catch
						{
							return (((IModManifest Mod, ManifestEntry Entry) ModEntry, NexusModModel Model)?)null;
						}
					}))
			);

			foreach (var maybeEntry in modDetails)
			{
				if (maybeEntry is not { } entry)
					continue;
				if (!SemanticVersionParser.TryParse(entry.Model.Version, out var version))
					continue;

				remainingMods.Remove(entry.ModEntry);
				results[entry.ModEntry.Mod] = (Version: version, UpdateInfo: $"https://www.nexusmods.com/cobaltcore/mods/{entry.Model.ID}");
				this.Database.ModIdToVersion[entry.Model.ID] = version;
			}

			// if we STILL have remaining mods, then we either exceeded the quota, or failed to get some versions for whatever other reason

			return results;
		}
		finally
		{
			this.Semaphore.Release();
		}
	}

	private async Task<IReadOnlyList<NexusModModel>> GetLatestAddedMods()
	{
		var stream = await this.Client.GetStreamAsync("https://api.nexusmods.com/v1/games/cobaltcore/mods/latest_added.json");
		using var streamReader = new StreamReader(stream);
		using var jsonReader = new JsonTextReader(streamReader);
		return this.Serializer.Deserialize<IReadOnlyList<NexusModModel>>(jsonReader) ?? throw new InvalidDataException();
	}

	private async Task<IReadOnlyList<NexusModModel>> GetLatestUpdatedMods()
	{
		var stream = await this.Client.GetStreamAsync("https://api.nexusmods.com/v1/games/cobaltcore/mods/latest_updated.json");
		using var streamReader = new StreamReader(stream);
		using var jsonReader = new JsonTextReader(streamReader);
		return this.Serializer.Deserialize<IReadOnlyList<NexusModModel>>(jsonReader) ?? throw new InvalidDataException();
	}

	private async Task<IReadOnlyList<NexusModModel>> GetTrendingMods()
	{
		var stream = await this.Client.GetStreamAsync("https://api.nexusmods.com/v1/games/cobaltcore/mods/trending.json");
		using var streamReader = new StreamReader(stream);
		using var jsonReader = new JsonTextReader(streamReader);
		return this.Serializer.Deserialize<IReadOnlyList<NexusModModel>>(jsonReader) ?? throw new InvalidDataException();
	}

	private async Task<IReadOnlyList<NexusModLastUpdateModel>> GetUpdatedMods(long timeSinceLastUpdate)
	{
		var period = timeSinceLastUpdate switch
		{
			>= 60 * 60 * 24 * 7 => "1m",
			>= 60 * 60 * 24 => "1w",
			_ => "1d"
		};

		var stream = await this.Client.GetStreamAsync($"https://api.nexusmods.com/v1/games/cobaltcore/mods/updated.json?period={period}");
		using var streamReader = new StreamReader(stream);
		using var jsonReader = new JsonTextReader(streamReader);
		return this.Serializer.Deserialize<IReadOnlyList<NexusModLastUpdateModel>>(jsonReader) ?? throw new InvalidDataException();
	}

	private async Task<NexusModModel> GetMod(int id)
	{
		var stream = await this.Client.GetStreamAsync($"https://api.nexusmods.com/v1/games/cobaltcore/mods/{id}.json");
		using var streamReader = new StreamReader(stream);
		using var jsonReader = new JsonTextReader(streamReader);
		return this.Serializer.Deserialize<NexusModModel>(jsonReader) ?? throw new InvalidDataException();
	}
}
