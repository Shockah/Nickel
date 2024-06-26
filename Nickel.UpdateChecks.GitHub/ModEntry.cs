using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nickel.UpdateChecks.GitHub;

public sealed class ModEntry : SimpleMod, IUpdateSource
{
	private const long UpdateCheckThrottleDuration = 60 * 5;

	internal static ModEntry Instance { get; private set; } = null!;

	private FileInfo DatabaseFile
		=> new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CobaltCore", NickelConstants.Name, $"{this.Package.Manifest.UniqueName}.json"));

	private readonly JsonSerializerSettings SerializerSettings;
	private readonly JsonSerializer Serializer;
	private HttpClient? Client;

	private readonly SemaphoreSlim Semaphore = new(1, 1);
	private Database Database = new();

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;

		this.SerializerSettings = new()
		{
			ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
			Formatting = Formatting.Indented,
		};
		this.SerializerSettings.Converters.Add(new SemanticVersionConverter());
		this.Serializer = JsonSerializer.Create(this.SerializerSettings);

		this.LoadDatabase();

		helper.ModRegistry.GetApi<IUpdateChecksApi>("Nickel.UpdateChecks")!.RegisterUpdateSource("GitHub", this);
	}

	private void LoadDatabase()
	{
		if (this.DatabaseFile.Exists)
		{
			try
			{
				using var stream = this.DatabaseFile.OpenRead();
				using var streamReader = new StreamReader(stream);
				using var jsonReader = new JsonTextReader(streamReader);
				this.Database = this.Serializer.Deserialize<Database>(jsonReader) ?? new();
			}
			catch
			{
				this.Database = new();
			}
		}
		else
		{
			this.Database = new();
			this.SaveDatabase();
		}

		this.Client = this.MakeHttpClient();
	}

	private void SaveDatabase()
	{
		try
		{
			this.DatabaseFile.Directory?.Create();
			using var stream = this.DatabaseFile.OpenWrite();
			using var streamWriter = new StreamWriter(stream);
			using var jsonWriter = new JsonTextWriter(streamWriter);
			this.Serializer.Serialize(jsonWriter, this.Database);
		}
		catch (Exception ex)
		{
			this.Logger.LogError("Could not save database file: {Exception}", ex);
		}
	}

	private HttpClient? MakeHttpClient()
	{
		var client = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(15)
		};

		client.DefaultRequestHeaders.Add("User-Agent", $"{this.Package.Manifest.UniqueName}/{this.Package.Manifest.Version}");
		client.DefaultRequestHeaders.Add("Application-Name", this.Package.Manifest.UniqueName);
		client.DefaultRequestHeaders.Add("Application-Version", this.Package.Manifest.Version.ToString());
		client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

		if (this.Database.Token is { } token)
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

		return client;
	}

	public bool TryParseManifestEntry(IModManifest mod, object? rawManifestEntry, out object? manifestEntry)
	{
		manifestEntry = null;

		if (JsonConvert.DeserializeObject<ManifestEntry>(JsonConvert.SerializeObject(rawManifestEntry, this.SerializerSettings), this.SerializerSettings) is not { } entry)
		{
			this.Logger.LogError("Cannot check GitHub updates for mod {ModName}: invalid `UpdateChecks` structure.", mod.GetDisplayName(@long: false));
			return false;
		}

		if (entry.Repository.Count(c => c == '/') != 1)
		{
			this.Logger.LogError("Cannot check GitHub updates for mod {ModName}: invalid `UpdateChecks` structure: provided `Repository` is not valid.", mod.GetDisplayName(@long: false));
			return false;
		}

		manifestEntry = entry;
		return true;
	}

	private static SemanticVersion? ParseVersionOrNull(string? versionString)
	{
		if (versionString is null)
			return null;

		var last = versionString.Split('/').Last();
		if (last.StartsWith('v'))
			last = last.Substring(1);

		return SemanticVersionParser.TryParse(last, out var version) ? version : null;
	}

	private static SemanticVersion? ParseVersionOrNull(GithubReleaseModel model, ManifestEntry entry)
	{
		try
		{
			if (!string.IsNullOrEmpty(entry.ReleaseTagRegex) && model.TagName is { } tag)
			{
				var match = new Regex(entry.ReleaseTagRegex).Match(tag);
				if (match.Success)
				{
					if (match.Groups.Count >= 2 && ParseVersionOrNull(match.Groups[1].Value) is { } group1Version)
						return group1Version;
					else if (ParseVersionOrNull(match.Value) is { } group0Version)
						return group0Version;
				}
			}
		}
		catch
		{
		}

		try
		{
			if (!string.IsNullOrEmpty(entry.ReleaseNameRegex))
			{
				var match = new Regex(entry.ReleaseNameRegex).Match(model.Name);
				if (match.Success)
				{
					if (match.Groups.Count >= 2 && ParseVersionOrNull(match.Groups[1].Value) is { } group1Version)
						return group1Version;
					else if (ParseVersionOrNull(match.Value) is { } group0Version)
						return group0Version;
				}
			}
		}
		catch
		{
		}

		return ParseVersionOrNull(model.TagName) ?? ParseVersionOrNull(model.Name);
	}

	public async Task<IReadOnlyDictionary<IModManifest, UpdateDescriptor>> GetLatestVersionsAsync(IEnumerable<(IModManifest Mod, object? ManifestEntry)> mods)
	{
		await this.Semaphore.WaitAsync();
		try
		{
			var results = new Dictionary<IModManifest, UpdateDescriptor>();
			var now = DateTimeOffset.Now.ToUnixTimeSeconds();
			var remainingMods = mods
				.Where(e => e.ManifestEntry is ManifestEntry)
				.Select(e => (Mod: e.Mod, Entry: (ManifestEntry)e.ManifestEntry!))
				.ToList();

			foreach (var modEntry in remainingMods)
				if (this.Database.UniqueNameToEntry.TryGetValue(modEntry.Mod.UniqueName, out var entry))
					results[modEntry.Mod] = new(entry.Version, [entry.Url]);

			if (now - this.Database.LastUpdate < UpdateCheckThrottleDuration)
			{
				this.Logger.LogDebug("Throttling GitHub update checks.");
				return results;
			}
			if (this.Client is not { } client)
			{
				this.Logger.LogError("Requested GitHub update checks, but HTTP client is not set up.");
				return results;
			}

			if (string.IsNullOrEmpty(this.Database.Token))
				this.Logger.LogWarning("Requested GitHub update checks, but no API key is provided in the `{File}` file - this can cause rate limit problems.", this.DatabaseFile.FullName);

			var repositoryReleases = await Task.WhenAll(
				remainingMods
					.GroupBy(modEntry => modEntry.Entry.Repository)
					.Select(group => Task.Run(async () =>
					{
						try
						{
							return (Repository: group.Key, ModEntries: group.ToList(), Releases: await this.GetReleases(client, group.Key));
						}
						catch
						{
							return (Repository: group.Key, ModEntries: group.ToList(), Releases: []);
						}
					}))
			);

			foreach (var (repository, modEntries, releases) in repositoryReleases)
			{
				foreach (var modEntry in modEntries)
				{
					var matchingReleases = releases
						.Select(release => (Release: release, Version: ParseVersionOrNull(release, modEntry.Entry)))
						.Where(e => e.Version is not null)
						.Select(e => (Release: e.Release, Version: e.Version!.Value))
						.ToList();

					if (matchingReleases.Count == 0)
						continue;
					var (release, version) = matchingReleases.MaxBy(e => e.Release.PublishedAt)!;

					remainingMods.Remove(modEntry);
					results[modEntry.Mod] = new(version, [release.Url]);
					this.Database.UniqueNameToEntry[modEntry.Mod.UniqueName] = new Database.Entry { Version = version, Url = release.Url };
				}
			}

			// if we still have remaining mods, then we either exceeded the quota, or failed to get some versions for whatever other reason

			this.Database.LastUpdate = now;
			return results;
		}
		finally
		{
			this.SaveDatabase();
			this.Semaphore.Release();
		}
	}

	private async Task<List<GithubReleaseModel>> GetReleases(HttpClient client, string repository)
	{
		try
		{
			this.Logger.LogDebug("Requesting releases for repository {Repository}...", repository);
			var stream = await client.GetStreamAsync($"https://api.github.com/repos/{repository}/releases?per_page=100");
			using var streamReader = new StreamReader(stream);
			using var jsonReader = new JsonTextReader(streamReader);
			return this.Serializer.Deserialize<List<GithubReleaseModel>>(jsonReader) ?? throw new InvalidDataException();
		}
		catch (Exception ex)
		{
			this.Logger.LogDebug("Failed to retrieve releases for repository {Repository}: {Error}", repository, ex.Message);
			throw;
		}
	}
}
