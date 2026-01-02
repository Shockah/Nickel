using FSPRO;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nickel.Common;
using Nickel.InfoScreens;
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
	private bool HasAnyMods;

	private readonly SemaphoreSlim Semaphore = new(1, 1);
	private readonly Database Database;

	private object IconSpriteEntry = null!;
	private object IconOnSpriteEntry = null!;

	private readonly IUpdateChecksApi UpdateChecksApi;
	private object? ModSettingsApi;
	private object? InfoScreensApi;

	private bool RequestedSetupInfoScreen;
	private bool RequestedUnauthorizedInfoScreen;
	private bool ShownSetupInfoScreen;
	private object? SetupInfoScreenEntry;

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
		
		this.UpdateChecksApi = this.Helper.ModRegistry.GetApi<IUpdateChecksApi>("Nickel.UpdateChecks")!;
		this.UpdateChecksApi.RegisterUpdateSource(SourceKey, this);

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;
			this.SetupAfterDbInit();
		};
	}

	private void SetupAfterDbInit()
	{
		this.ModSettingsApi = this.Helper.ModRegistry.GetApi<IModSettingsApi>("Nickel.ModSettings");
		this.InfoScreensApi = this.Helper.ModRegistry.GetApi<IInfoScreensApi>("Nickel.InfoScreens");
		
		this.IconSpriteEntry = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/ProviderIcon.png"));
		this.IconOnSpriteEntry = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/ProviderIconOn.png"));
		
		if (this.Helper.ModRegistry.LoadedMods.ContainsKey("Nickel.UpdateChecks.UI") && this.ModSettingsApi is IModSettingsApi settingsApi)
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
				]).SubscribeToOnMenuClose(g =>
				{
					this.SaveSettings();
					if (!this.Database.IsEnabled || !string.IsNullOrEmpty(this.Database.ApiKey))
						(this.SetupInfoScreenEntry as IInfoScreensApi.IInfoScreenEntry)?.Cancel(g);
					
					this.Client = this.MakeHttpClient();
					this.UpdateChecksApi.RequestUpdateInfo(this);
				})
			);
		
		if (this.RequestedSetupInfoScreen)
			this.FinallyShowSetupInfoScreen();
		else
			this.RequestSetupInfoScreen();
		
		if (this.RequestedUnauthorizedInfoScreen)
			this.FinallyShowUnauthorizedInfoScreen();
	}
	
	private void SaveSettings()
		=> this.Helper.Storage.SaveJson(this.DatabaseFile, this.Database);

	private void RequestSetupInfoScreen()
	{
		if (this.ShownSetupInfoScreen)
			return;
		
		if (this.Helper.Events.ModLoadPhaseState is not { Phase: ModLoadPhase.AfterDbInit, IsDone: true })
		{
			this.RequestedSetupInfoScreen = true;
			return;
		}
		
		this.FinallyShowSetupInfoScreen();
	}

	private void FinallyShowSetupInfoScreen()
	{
		if (!this.Helper.ModRegistry.LoadedMods.ContainsKey("Nickel.UpdateChecks.UI"))
			return;
		if (this.InfoScreensApi is not IInfoScreensApi infoScreensApi)
			return;
		if (this.ModSettingsApi is not IModSettingsApi modSettingsApi)
			return;
		if (!this.Database.IsEnabled)
			return;
		if (!this.HasAnyMods)
			return;
		if (!string.IsNullOrEmpty(this.Database.ApiKey))
			return;
		
		if (this.ShownSetupInfoScreen)
			return;
		this.ShownSetupInfoScreen = true;
		this.RequestedSetupInfoScreen = false;
		
		var route = infoScreensApi.CreateBasicInfoScreenRoute();
		route.Paragraphs = [
			infoScreensApi.CreateBasicInfoScreenParagraph(this.Localizations.Localize(["infoScreen", "title"])).SetFont(DB.thicket),
			infoScreensApi.CreateBasicInfoScreenParagraph(this.Localizations.Localize(["infoScreen", "description", "setup"])).SetColor(Colors.textMain),
		];
		route.Actions = [
			infoScreensApi.CreateBasicInfoScreenAction(this.Localizations.Localize(["infoScreen", "actions", "details"]), args =>
			{
				Audio.Play(Event.Click);
				args.Route.RouteOverride = modSettingsApi.MakeModSettingsRouteForMod(this.Package.Manifest);
			}),
			infoScreensApi.CreateBasicInfoScreenAction(this.Localizations.Localize(["infoScreen", "actions", "remindLater"]), args =>
			{
				Audio.Play(Event.Click);
				args.G.CloseRoute(args.Route.AsRoute);
			}).SetControllerKeybind(Btn.B),
			infoScreensApi.CreateBasicInfoScreenAction(this.Localizations.Localize(["infoScreen", "actions", "disable"]), args =>
			{
				Audio.Play(Event.Click);

				this.Database.IsEnabled = false;
				this.SaveSettings();

				args.G.CloseRoute(args.Route.AsRoute);
			}).SetColor(Colors.redd).SetRequiresConfirmation(true),
		];

		this.SetupInfoScreenEntry = infoScreensApi.RequestInfoScreen("Setup", route.AsRoute, 1_000);
	}

	private void RequestUnauthorizedInfoScreen()
	{
		if (this.Helper.Events.ModLoadPhaseState is not { Phase: ModLoadPhase.AfterDbInit, IsDone: true })
		{
			this.RequestedUnauthorizedInfoScreen = true;
			return;
		}
		
		this.FinallyShowUnauthorizedInfoScreen();
	}

	private void FinallyShowUnauthorizedInfoScreen()
	{
		if (!this.Helper.ModRegistry.LoadedMods.ContainsKey("Nickel.UpdateChecks.UI"))
			return;
		if (this.InfoScreensApi is not IInfoScreensApi infoScreensApi)
			return;
		if (this.ModSettingsApi is not IModSettingsApi modSettingsApi)
			return;
		if (!this.Database.IsEnabled)
			return;
		if (!this.HasAnyMods)
			return;
		if (!string.IsNullOrEmpty(this.Database.ApiKey))
			return;
		if (!this.GotUnauthorized)
			return;
		
		this.RequestedUnauthorizedInfoScreen = false;
		
		var route = infoScreensApi.CreateBasicInfoScreenRoute();
		route.Paragraphs = [
			infoScreensApi.CreateBasicInfoScreenParagraph(this.Localizations.Localize(["infoScreen", "title"])).SetFont(DB.thicket),
			infoScreensApi.CreateBasicInfoScreenParagraph(this.Localizations.Localize(["infoScreen", "description", "unauthorized"])).SetColor(Colors.textMain),
		];
		route.Actions = [
			infoScreensApi.CreateBasicInfoScreenAction(this.Localizations.Localize(["infoScreen", "actions", "details"]), args =>
			{
				Audio.Play(Event.Click);
				args.Route.RouteOverride = modSettingsApi.MakeModSettingsRouteForMod(this.Package.Manifest);
			}),
			infoScreensApi.CreateBasicInfoScreenAction(this.Localizations.Localize(["infoScreen", "actions", "remindLater"]), args =>
			{
				Audio.Play(Event.Click);
				args.G.CloseRoute(args.Route.AsRoute);
			}).SetControllerKeybind(Btn.B),
			infoScreensApi.CreateBasicInfoScreenAction(this.Localizations.Localize(["infoScreen", "actions", "disable"]), args =>
			{
				Audio.Play(Event.Click);

				this.Database.IsEnabled = false;
				this.SaveSettings();

				args.G.CloseRoute(args.Route.AsRoute);
			}).SetColor(Colors.redd).SetRequiresConfirmation(true),
		];

		infoScreensApi.RequestInfoScreen("Unauthorized", route.AsRoute, 1_000);
	}

	private HttpClient MakeHttpClient()
	{
		var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
		client.DefaultRequestHeaders.Add("User-Agent", $"{this.Package.Manifest.UniqueName}/{this.Package.Manifest.Version}");
		client.DefaultRequestHeaders.Add("Application-Name", this.Package.Manifest.UniqueName);
		client.DefaultRequestHeaders.Add("Application-Version", this.Package.Manifest.Version.ToString());

		if (this.Database.ApiKey is { } apiKey)
			client.DefaultRequestHeaders.Add("apikey", apiKey);

		return client;
	}

	[UsedImplicitly]
	public IEnumerable<UpdateSourceMessage> Messages
	{
		get
		{
			if (!this.Database.IsEnabled)
				yield break;
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
		if (manifestEntry is not null && !this.HasAnyMods)
		{
			this.HasAnyMods = true;
			this.RequestSetupInfoScreen();
		}
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
			if (!this.Database.IsEnabled)
				return results;
			
			if (this.Client is not { } client || this.Database.ApiKey is null)
			{
				this.Logger.LogWarning("Requested NexusMods update checks, but no API key is provided in the `{File}` file.", this.DatabaseFile.FullName);
				return results;
			}
			
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
			{
				this.GotUnauthorized = true;
				this.RequestUnauthorizedInfoScreen();
			}

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
			{
				this.GotUnauthorized = true;
				this.RequestUnauthorizedInfoScreen();
			}

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
			{
				this.GotUnauthorized = true;
				this.RequestUnauthorizedInfoScreen();
			}

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
			{
				this.GotUnauthorized = true;
				this.RequestUnauthorizedInfoScreen();
			}

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
			{
				this.GotUnauthorized = true;
				this.RequestUnauthorizedInfoScreen();
			}

			this.Logger.LogDebug("Failed to retrieve mod {ModId}: {Error}", id, ex.Message);
			throw;
		}
	}
}
