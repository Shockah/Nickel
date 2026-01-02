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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nickel.UpdateChecks.GitHub;

public sealed class ModEntry : SimpleMod, IUpdateSource
{
	private const string SourceKey = "GitHub";
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
						setting: settingsApi.MakeList([
							new TokenModSetting
							{
								Title = () => this.Localizations.Localize(["modSettings", "token", "name"]),
								HasValue = () => !string.IsNullOrEmpty(this.Database.Token),
								PasteAction = (_, _, text) => this.Database.Token = text,
								SetupAction = (_, _) => MainMenu.TryOpenWebsiteLink("https://github.com/settings/tokens?type=beta"),
								BaseTooltips = () => [
									new GlossaryTooltip($"settings.{this.Package.Manifest.UniqueName}::Token")
									{
										TitleColor = Colors.textBold,
										Title = this.Localizations.Localize(["modSettings", "token", "name"]),
										Description = this.Localizations.Localize(["modSettings", "token", "description"])
									}
								]
							},
							settingsApi.MakeConditional(
								setting: settingsApi.MakeCheckbox(
									title: () => this.Localizations.Localize(["modSettings", "warnOnNoToken", "name"]),
									getter: () => this.Database.WarnOnNoToken,
									setter: (_, _, value) => this.Database.WarnOnNoToken = value
								).SetTooltips(() => [
									new GlossaryTooltip($"settings.{this.Package.Manifest.UniqueName}::WarnOnNoToken")
									{
										TitleColor = Colors.textBold,
										Title = this.Localizations.Localize(["modSettings", "warnOnNoToken", "name"]),
										Description = this.Localizations.Localize(["modSettings", "warnOnNoToken", "description"])
									}
								]),
								isVisible: () => string.IsNullOrEmpty(this.Database.Token)
							)
						]),
						isVisible: () => this.Database.IsEnabled
					),
					settingsApi.MakeButton(
						() => this.Localizations.Localize(["modSettings", "guide", "name"]),
						(_, _) => MainMenu.TryOpenWebsiteLink("https://github.com/Shockah/Nickel/blob/master/docs/update-checks.md#github")
					)
				]).SubscribeToOnMenuClose(g =>
				{
					this.SaveSettings();
					if (!this.Database.IsEnabled || !string.IsNullOrEmpty(this.Database.Token) || !this.Database.WarnOnNoToken)
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
		if (!string.IsNullOrEmpty(this.Database.Token))
			return;
		if (!this.Database.WarnOnNoToken)
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
			infoScreensApi.CreateBasicInfoScreenAction(this.Localizations.Localize(["infoScreen", "actions", "acknowledge"]), args =>
			{
				Audio.Play(Event.Click);

				this.Database.WarnOnNoToken = false;
				this.SaveSettings();

				args.G.CloseRoute(args.Route.AsRoute);
			}).SetColor(Colors.cheevoGold).SetRequiresConfirmation(true),
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
		if (!string.IsNullOrEmpty(this.Database.Token))
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
			infoScreensApi.CreateBasicInfoScreenAction(this.Localizations.Localize(["infoScreen", "actions", "acknowledge"]), args =>
			{
				Audio.Play(Event.Click);

				this.Database.WarnOnNoToken = false;
				this.SaveSettings();

				args.G.CloseRoute(args.Route.AsRoute);
			}).SetColor(Colors.cheevoGold).SetRequiresConfirmation(true),
			infoScreensApi.CreateBasicInfoScreenAction(this.Localizations.Localize(["infoScreen", "actions", "disable"]), args =>
			{
				Audio.Play(Event.Click);

				this.Database.IsEnabled = false;
				this.SaveSettings();

				args.G.CloseRoute(args.Route.AsRoute);
			}).SetColor(Colors.redd).SetRequiresConfirmation(true),
		];

		infoScreensApi.RequestInfoScreen("Unauthorized", route.AsRoute, 999);
	}

	private HttpClient MakeHttpClient()
	{
		var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
		client.DefaultRequestHeaders.Add("User-Agent", $"{this.Package.Manifest.UniqueName}/{this.Package.Manifest.Version}");
		client.DefaultRequestHeaders.Add("Application-Name", this.Package.Manifest.UniqueName);
		client.DefaultRequestHeaders.Add("Application-Version", this.Package.Manifest.Version.ToString());
		client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

		if (this.Database.Token is { } token)
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

		this.GotUnauthorized = false;
		return client;
	}

	public bool TryParseManifestEntry(IModManifest mod, JObject rawManifestEntry, out object? manifestEntry)
	{
		manifestEntry = null;

		if (rawManifestEntry.ToObject<ManifestEntry>(this.Helper.Storage.JsonSerializer) is not { } entry)
		{
			this.Logger.LogError("Cannot check GitHub updates for mod {ModName}: invalid `UpdateChecks` structure.", mod.GetDisplayName(@long: false));
			return false;
		}

		if (entry.Repository.Count(c => c == '/') != 1)
		{
			this.Logger.LogError("Cannot check GitHub updates for mod {ModName}: invalid `UpdateChecks` structure: provided `Repository` is not valid.", mod.GetDisplayName(@long: false));
			return false;
		}

		this.HasAnyMods = true;
		this.RequestSetupInfoScreen();
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
					if (ParseVersionOrNull(match.Value) is { } group0Version)
						return group0Version;
				}
			}
		}
		catch
		{
			// ignored
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
					if (ParseVersionOrNull(match.Value) is { } group0Version)
						return group0Version;
				}
			}
		}
		catch
		{
			// ignored
		}

		return ParseVersionOrNull(model.TagName) ?? ParseVersionOrNull(model.Name);
	}
	
	[UsedImplicitly]
	public string Name
		=> "GitHub";

	[UsedImplicitly]
	public Spr? GetIcon(IModManifest mod, UpdateDescriptor descriptor, bool hover)
		=> ((ISpriteEntry)(hover ? this.IconOnSpriteEntry : this.IconSpriteEntry)).Sprite;

	[UsedImplicitly]
	public IEnumerable<Tooltip> GetVisitWebsiteTooltips(IModManifest mod, UpdateDescriptor descriptor)
		=> [
			new GlossaryTooltip($"settings.{this.Package.Manifest.UniqueName}::VisitWebsite::GitHub")
			{
				TitleColor = Colors.textBold,
				Title = this.Localizations.Localize(["visitWebsite", "title"]),
				Description = this.Localizations.Localize(["visitWebsite", "description"])
			}
		];

	public async Task<IReadOnlyDictionary<IModManifest, UpdateDescriptor>> GetLatestVersionsAsync(IEnumerable<(IModManifest Mod, object? ManifestEntry)> mods)
	{
		await this.Semaphore.WaitAsync();
		try
		{
			var results = new Dictionary<IModManifest, UpdateDescriptor>();
			if (!this.Database.IsEnabled)
				return results;
			
			if (this.Client is not { } client)
			{
				this.Logger.LogError("Requested GitHub update checks, but HTTP client is not set up.");
				return results;
			}

			if (string.IsNullOrEmpty(this.Database.Token))
				this.Logger.LogWarning("Requested GitHub update checks, but no API key is provided in the `{File}` file - this can cause rate limit problems.", this.DatabaseFile.FullName);
			
			var now = DateTimeOffset.Now.ToUnixTimeSeconds();
			var remainingMods = mods
				.Where(e => e.ManifestEntry is ManifestEntry)
				.Select(e => (Mod: e.Mod, Entry: (ManifestEntry)e.ManifestEntry!))
				.ToList();

			foreach (var modEntry in remainingMods)
				if (this.Database.UniqueNameToEntry.TryGetValue(modEntry.Mod.UniqueName, out var entry))
					results[modEntry.Mod] = new(SourceKey, entry.Version, entry.Url);

			if (now - this.Database.LastUpdate < UpdateCheckThrottleDuration)
			{
				this.Logger.LogDebug("Throttling GitHub update checks.");
				return results;
			}

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

			foreach (var (_, modEntries, releases) in repositoryReleases)
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
					var (release, version) = matchingReleases.MaxBy(e => e.Release.PublishedAt);

					remainingMods.Remove(modEntry);
					results[modEntry.Mod] = new(SourceKey, version, release.Url);
					this.Database.UniqueNameToEntry[modEntry.Mod.UniqueName] = new Database.Entry { Version = version, Url = release.Url };
				}
			}

			// if we still have remaining mods, then we either exceeded the quota, or failed to get some versions for whatever other reason

			this.Database.LastUpdate = now;
			return results;
		}
		finally
		{
			this.Helper.Storage.SaveJson(this.DatabaseFile, this.Database);
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
			await using var jsonReader = new JsonTextReader(streamReader);
			return this.Helper.Storage.JsonSerializer.Deserialize<List<GithubReleaseModel>>(jsonReader) ?? throw new InvalidDataException();
		}
		catch (Exception ex)
		{
			if (ex is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized })
			{
				this.GotUnauthorized = true;
				this.RequestUnauthorizedInfoScreen();
			}

			this.Logger.LogDebug("Failed to retrieve releases for repository {Repository}: {Error}", repository, ex.Message);
			throw;
		}
	}
}
