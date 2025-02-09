using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nickel.Common;
using Nickel.ModSettings;
using Nickel.UpdateChecks.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Nickel.UpdateChecks.NexusMods;

public sealed class ModEntry : SimpleMod, IUpdateSource
{
	private const string SourceKey = "NexusMods";
	private const long UpdateCheckThrottleDuration = 60 * 5;

	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	private IWritableFileInfo DatabaseFile
		=> this.Helper.Storage.GetMainPrivateStorageFile("json");

	private HttpClient? Client;
	private bool GotUnauthorized;

	private readonly SemaphoreSlim Semaphore = new(1, 1);
	private readonly Database Database;

	private object IconSpriteEntry = null!;
	private object IconOnSpriteEntry = null!;

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(
				new JsonLocalizationProvider(
					tokenExtractor: new SimpleLocalizationTokenExtractor(),
					localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
				)
			)
		);
		helper.Storage.ApplyJsonSerializerSettings(s => s.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor);
		this.Database = helper.Storage.LoadJson<Database>(this.DatabaseFile);
		this.Client = this.MakeHttpClient();
		
		helper.ModRegistry.GetApi<IUpdateChecksApi>("Nickel.UpdateChecks")!.RegisterUpdateSource(SourceKey, this);

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;
			this.SetupAfterDbInit();
		};
	}

	private void SetupAfterDbInit()
	{
		this.IconSpriteEntry = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/ProviderIcon.png"));
		this.IconOnSpriteEntry = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/ProviderIconOn.png"));
		
		var updateChecksApi = this.Helper.ModRegistry.GetApi<IUpdateChecksApi>("Nickel.UpdateChecks")!;
		if (this.Helper.ModRegistry.GetApi<IModSettingsApi>("Nickel.ModSettings") is { } settingsApi)
			settingsApi.RegisterModSettings(
				settingsApi.MakeList([
					settingsApi.MakeCheckbox(
						title: () => this.Localizations.Localize(["modSettings", "enabled", "name"]),
						getter: () => this.Database.IsEnabled,
						setter: (_, _, value) => this.Database.IsEnabled = value
					),
					settingsApi.MakeConditional(
						setting: new TokenModSetting
						{
							Title = () => this.Localizations.Localize(["modSettings", "apiKey", "name"]),
							HasValue = () => !string.IsNullOrEmpty(this.Database.ApiKey),
							PasteAction = (_, _, text) => this.Database.ApiKey = text,
							SetupAction = (_, _) => MainMenu.TryOpenWebsiteLink("https://next.nexusmods.com/settings/api-keys"),
							BaseTooltips = () => [
								new GlossaryTooltip($"settings.{this.Package.Manifest.UniqueName}::ApiKey")
								{
									TitleColor = Colors.textBold,
									Title = this.Localizations.Localize(["modSettings", "apiKey", "name"]),
									Description = this.Localizations.Localize(["modSettings", "apiKey", "description"])
								}
							]
						},
						isVisible: () => this.Database.IsEnabled
					),
					settingsApi.MakeButton(
						() => this.Localizations.Localize(["modSettings", "guide", "name"]),
						(_, _) => MainMenu.TryOpenWebsiteLink("https://github.com/Shockah/Nickel/blob/master/docs/update-checks.md#nexusmods")
					)
				]).SubscribeToOnMenuClose(_ =>
				{
					this.Helper.Storage.SaveJson(this.DatabaseFile, this.Database);
					this.Client = this.MakeHttpClient();

					if (this.Database.IsEnabled)
						updateChecksApi.RequestUpdateInfo(this);
				})
			);
	}

	private HttpClient MakeHttpClient()
	{
		var client = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(15)
		};

		client.DefaultRequestHeaders.Add("User-Agent", $"{this.Package.Manifest.UniqueName}/{this.Package.Manifest.Version}");
		client.DefaultRequestHeaders.Add("Application-Name", this.Package.Manifest.UniqueName);
		client.DefaultRequestHeaders.Add("Application-Version", this.Package.Manifest.Version.ToString());

		if (this.Database.ApiKey is { } apiKey)
			client.DefaultRequestHeaders.Add("apikey", apiKey);

		return client;
	}

	public IEnumerable<UpdateSourceMessage> Messages
	{
		get
		{
			if (string.IsNullOrEmpty(this.Database.ApiKey))
				yield return new(UpdateSourceMessageLevel.Error, this.Localizations.Localize(["message", "missingApiKey"]));
			if (this.GotUnauthorized)
				yield return new(UpdateSourceMessageLevel.Error, this.Localizations.Localize(["message", "unauthorized"]));
		}
	}
	
	[UsedImplicitly]
	public string Name
		=> "NexusMods";

	[UsedImplicitly]
	public Spr? GetIcon(IModManifest mod, UpdateDescriptor descriptor, bool hover)
		=> ((ISpriteEntry)(hover ? this.IconOnSpriteEntry : this.IconSpriteEntry)).Sprite;

	[UsedImplicitly]
	public IEnumerable<Tooltip> GetVisitWebsiteTooltips(IModManifest mod, UpdateDescriptor descriptor)
		=> [
			new GlossaryTooltip($"settings.{this.Package.Manifest.UniqueName}::VisitWebsite::NexusMods")
			{
				TitleColor = Colors.textBold,
				Title = this.Localizations.Localize(["visitWebsite", "title"]),
				Description = this.Localizations.Localize(["visitWebsite", "description"])
			}
		];

	public bool TryParseManifestEntry(IModManifest mod, JObject rawManifestEntry, out object? manifestEntry)
	{
		manifestEntry = rawManifestEntry.ToObject<ManifestEntry>(this.Helper.Storage.JsonSerializer);
		if (manifestEntry is null)
			this.Logger.LogError("Cannot check NexusMods updates for mod {ModName}: invalid `UpdateChecks` structure.", mod.GetDisplayName(@long: false));
		return manifestEntry is not null;
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
				if (this.Database.ModIdToVersion.TryGetValue(modEntry.Entry.Id, out var version))
					results[modEntry.Mod] = new(SourceKey, version, $"https://www.nexusmods.com/cobaltcore/mods/{modEntry.Entry.Id}");

			if (now - this.Database.LastUpdate < UpdateCheckThrottleDuration)
			{
				this.Logger.LogDebug("Throttling NexusMods update checks.");
				return results;
			}
			if (this.Client is not { } client || this.Database.ApiKey is null)
			{
				this.Logger.LogWarning("Requested NexusMods update checks, but no API key is provided in the `{File}` file.", this.DatabaseFile.FullName);
				return results;
			}

			// if we had a previous update within the last month, we can try checking if any of the remaining mods had any updates and return early if not
			// if we only have 3 mods to fetch, we skip to save on requests

			try
			{
				if (remainingMods.Count >= 3)
				{
					var timeSinceLastUpdate = now - this.Database.LastUpdate;
					if (timeSinceLastUpdate < 60 * 60 * 24 * 28)
					{
						var updatedMods = await this.GetUpdatedMods(client, timeSinceLastUpdate);

						foreach (var modEntry in remainingMods.ToList())
						{
							if (updatedMods.Any(model => model.Id == modEntry.Entry.Id))
								continue;
							if (!this.Database.ModIdToVersion.ContainsKey(modEntry.Entry.Id))
								continue;

							remainingMods.Remove(modEntry);
						}

						if (remainingMods.Count == 0)
							return results;
					}
				}
			}
			catch
			{
				// ignored
			}

			// updating version data from the 3 10-element lists
			// if we only have 3 mods to fetch, we skip to save on requests

			try
			{
				if (remainingMods.Count >= 3)
				{
					var latestAddedModsTask = this.GetLatestAddedMods(client);
					var latestUpdatedModsTask = this.GetLatestUpdatedMods(client);
					var trendingModsTask = this.GetTrendingMods(client);

					var latestAddedMods = await latestAddedModsTask;
					var latestUpdatedMods = await latestUpdatedModsTask;
					var trendingMods = await trendingModsTask;

					foreach (var model in latestAddedMods.Concat(latestUpdatedMods).Concat(trendingMods))
					{
						if (ParseVersionOrNull(model.Version) is not { } version)
							continue;

						this.Database.ModIdToVersion[model.Id] = version;

						if (remainingMods.FirstOrNull(e => e.Entry.Id == model.Id) is not { } modEntry)
							continue;

						remainingMods.Remove(modEntry);
						results[modEntry.Mod] = new(SourceKey, version, $"https://www.nexusmods.com/cobaltcore/mods/{model.Id}");
					}

					if (remainingMods.Count == 0)
						return results;
				}
			}
			catch
			{
				// ignored
			}

			// if we still have some remaining mods, we gotta fetch them 1-by-1

			var modDetails = await Task.WhenAll(
				remainingMods
					.Select(modEntry => Task.Run(async () =>
					{
						try
						{
							return (ModEntry: modEntry, Model: await this.GetMod(client, modEntry.Entry.Id));
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
				if (ParseVersionOrNull(entry.Model.Version) is not { } version)
					continue;

				remainingMods.Remove(entry.ModEntry);
				results[entry.ModEntry.Mod] = new(SourceKey, version, $"https://www.nexusmods.com/cobaltcore/mods/{entry.Model.Id}");
				this.Database.ModIdToVersion[entry.Model.Id] = version;
			}

			// if we STILL have remaining mods, then we either exceeded the quota, or failed to get some versions for whatever other reason

			this.Database.LastUpdate = now;
			return results;
		}
		finally
		{
			this.Helper.Storage.SaveJson(this.DatabaseFile, this.Database);
			this.Semaphore.Release();
		}
	}

	private async Task<IReadOnlyList<NexusModModel>> GetLatestAddedMods(HttpClient client)
	{
		try
		{
			this.Logger.LogDebug("Requesting latest added mods...");
			var stream = await client.GetStreamAsync("https://api.nexusmods.com/v1/games/cobaltcore/mods/latest_added.json");
			using var streamReader = new StreamReader(stream);
			await using var jsonReader = new JsonTextReader(streamReader);
			return this.Helper.Storage.JsonSerializer.Deserialize<IReadOnlyList<NexusModModel>>(jsonReader) ?? throw new InvalidDataException();
		}
		catch (Exception ex)
		{
			if (ex is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized })
				this.GotUnauthorized = true;

			this.Logger.LogDebug("Failed to retrieve latest added mods: {Error}", ex.Message);
			throw;
		}
	}

	private async Task<IReadOnlyList<NexusModModel>> GetLatestUpdatedMods(HttpClient client)
	{
		try
		{
			this.Logger.LogDebug("Requesting latest updated mods...");
			var stream = await client.GetStreamAsync("https://api.nexusmods.com/v1/games/cobaltcore/mods/latest_updated.json");
			using var streamReader = new StreamReader(stream);
			await using var jsonReader = new JsonTextReader(streamReader);
			return this.Helper.Storage.JsonSerializer.Deserialize<IReadOnlyList<NexusModModel>>(jsonReader) ?? throw new InvalidDataException();
		}
		catch (Exception ex)
		{
			if (ex is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized })
				this.GotUnauthorized = true;

			this.Logger.LogDebug("Failed to retrieve latest updated mods: {Error}", ex.Message);
			throw;
		}
	}

	private async Task<IReadOnlyList<NexusModModel>> GetTrendingMods(HttpClient client)
	{
		try
		{
			this.Logger.LogDebug("Requesting trending mods...");
			var stream = await client.GetStreamAsync("https://api.nexusmods.com/v1/games/cobaltcore/mods/trending.json");
			using var streamReader = new StreamReader(stream);
			await using var jsonReader = new JsonTextReader(streamReader);
			return this.Helper.Storage.JsonSerializer.Deserialize<IReadOnlyList<NexusModModel>>(jsonReader) ?? throw new InvalidDataException();
		}
		catch (Exception ex)
		{
			if (ex is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized })
				this.GotUnauthorized = true;

			this.Logger.LogDebug("Failed to retrieve trending mods: {Error}", ex.Message);
			throw;
		}
	}

	private async Task<IReadOnlyList<NexusModLastUpdateModel>> GetUpdatedMods(HttpClient client, long timeSinceLastUpdate)
	{
		var period = timeSinceLastUpdate switch
		{
			>= 60 * 60 * 24 * 7 => "1m",
			>= 60 * 60 * 24 => "1w",
			_ => "1d"
		};

		try
		{
			this.Logger.LogDebug("Requesting updated mods in the {Period} period...", period);
			var stream = await client.GetStreamAsync($"https://api.nexusmods.com/v1/games/cobaltcore/mods/updated.json?period={period}");
			using var streamReader = new StreamReader(stream);
			await using var jsonReader = new JsonTextReader(streamReader);
			return this.Helper.Storage.JsonSerializer.Deserialize<IReadOnlyList<NexusModLastUpdateModel>>(jsonReader) ?? throw new InvalidDataException();
		}
		catch (Exception ex)
		{
			if (ex is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized })
				this.GotUnauthorized = true;

			this.Logger.LogDebug("Failed to retrieve updated mods in the {Period} period: {Error}", period, ex.Message);
			throw;
		}
	}

	private async Task<NexusModModel> GetMod(HttpClient client, int id)
	{
		try
		{
			this.Logger.LogDebug("Requesting mod {ModId}...", id);
			var stream = await client.GetStreamAsync($"https://api.nexusmods.com/v1/games/cobaltcore/mods/{id}.json");
			using var streamReader = new StreamReader(stream);
			await using var jsonReader = new JsonTextReader(streamReader);
			return this.Helper.Storage.JsonSerializer.Deserialize<NexusModModel>(jsonReader) ?? throw new InvalidDataException();
		}
		catch (Exception ex)
		{
			if (ex is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized })
				this.GotUnauthorized = true;

			this.Logger.LogDebug("Failed to retrieve mod {ModId}: {Error}", id, ex.Message);
			throw;
		}
	}
}
