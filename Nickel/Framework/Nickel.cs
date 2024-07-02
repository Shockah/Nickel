using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager.Cecil;
using Nanoray.Shrike;
using Nickel.Common;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Nickel;

internal sealed partial class Nickel(LaunchArguments launchArguments)
{
	internal static Nickel Instance { get; private set; } = null!;
	internal Harmony? Harmony { get; private set; }
	internal ModManager ModManager { get; private set; } = null!;
	private LaunchArguments LaunchArguments { get; } = launchArguments;
	internal DebugMode DebugMode { get; private set; } = DebugMode.Disabled;
	private Settings Settings = new();
	
	private SaveManager SaveManager = null!;

	internal static int Main(string[] args)
	{
		Option<bool> vanillaOption = new(
			name: "--vanilla",
			description: "Whether to run the vanilla game instead.",
			getDefaultValue: () => false
		)
		{
			Arity = ArgumentArity.ZeroOrOne
		};
		Option<bool?> debugOption = new(
			name: "--debug",
			description: "Whether the game should be ran in debug mode."
		);
		Option<bool?> saveInDebugOption = new(
			name: "--saveInDebug",
			description: "Whether the game should be auto-saved even in debug mode."
		);
		Option<bool?> initSteamOption = new(
			name: "--initSteam",
			description: "Whether Steam integration should be enabled."
		);
		Option<FileInfo?> gamePathOption = new(
			name: "--gamePath",
			description: "The path to CobaltCore.exe."
		);
		Option<DirectoryInfo?> modsPathOption = new(
			name: "--modsPath",
			description: "The path containing the mods to load."
		);
		Option<DirectoryInfo?> modStoragePathOption = new(
			name: "--modStoragePath",
			description: "The path containing mod data, like settings (ones that are fine to share)."
		);
		Option<DirectoryInfo?> privateModStoragePathOption = new(
			name: "--privateModStoragePath",
			description: "The path containing private mod data, like settings (ones that should never be shared)."
		);
		Option<DirectoryInfo?> savePathOption = new(
			name: "--savePath",
			description: "The path that will store the save data."
		);
		Option<DirectoryInfo?> logPathOption = new(
			name: "--logPath",
			description: "The folder logs will be stored in."
		);
		Option<bool?> timestampedLogFiles = new(
			name: "--keepLogs",
			description: "Uses timestamps for log filenames."
		);
		Option<string?> logPipeNameOption = new(
			name: "--logPipeName"
		)
		{ IsHidden = true };

		RootCommand rootCommand = new(NickelConstants.IntroMessage);
		rootCommand.AddOption(vanillaOption);
		rootCommand.AddOption(debugOption);
		rootCommand.AddOption(saveInDebugOption);
		rootCommand.AddOption(initSteamOption);
		rootCommand.AddOption(gamePathOption);
		rootCommand.AddOption(modsPathOption);
		rootCommand.AddOption(modStoragePathOption);
		rootCommand.AddOption(privateModStoragePathOption);
		rootCommand.AddOption(savePathOption);
		rootCommand.AddOption(logPathOption);
		rootCommand.AddOption(timestampedLogFiles);
		rootCommand.AddOption(logPipeNameOption);

		rootCommand.SetHandler(context =>
		{
			LaunchArguments launchArguments = new()
			{
				Vanilla = context.ParseResult.GetValueForOption(vanillaOption),
				Debug = context.ParseResult.GetValueForOption(debugOption),
				SaveInDebug = context.ParseResult.GetValueForOption(saveInDebugOption),
				InitSteam = context.ParseResult.GetValueForOption(initSteamOption),
				GamePath = context.ParseResult.GetValueForOption(gamePathOption),
				ModsPath = context.ParseResult.GetValueForOption(modsPathOption),
				ModStoragePath = context.ParseResult.GetValueForOption(modStoragePathOption),
				PrivateModStoragePath = context.ParseResult.GetValueForOption(privateModStoragePathOption),
				SavePath = context.ParseResult.GetValueForOption(savePathOption),
				LogPath = context.ParseResult.GetValueForOption(logPathOption),
				TimestampedLogFiles = context.ParseResult.GetValueForOption(timestampedLogFiles),
				LogPipeName = context.ParseResult.GetValueForOption(logPipeNameOption),
				UnmatchedArguments = context.ParseResult.UnmatchedTokens
			};
			context.ExitCode = CreateAndStartInstance(launchArguments);
		});
		return rootCommand.Invoke(args);
	}

	private static int CreateAndStartInstance(LaunchArguments launchArguments)
	{
		var realOut = Console.Out;
		var loggerFactory = LoggerFactory.Create(builder =>
		{
			if (string.IsNullOrEmpty(launchArguments.LogPipeName))
			{
				builder.SetMinimumLevel(LogLevel.Debug);
				var fileLogDirectory = launchArguments.LogPath ?? GetOrCreateDefaultLogDirectory();
				var timestampedLogFiles = launchArguments.TimestampedLogFiles ?? false;
				builder.AddProvider(FileLoggerProvider.CreateNewLog(LogLevel.Debug, fileLogDirectory, timestampedLogFiles));
				builder.AddProvider(new ConsoleLoggerProvider(LogLevel.Information, realOut, disposeWriter: false));
			}
			else
			{
				builder.SetMinimumLevel(LogLevel.Trace);
				builder.AddProvider(new NamedPipeClientLoggerProvider(launchArguments.LogPipeName));
			}
		});
		var logger = loggerFactory.CreateLogger(NickelConstants.Name);
		Console.SetOut(new LoggerTextWriter(logger, LogLevel.Information, realOut));
		Console.SetError(new LoggerTextWriter(logger, LogLevel.Error, Console.Error));
		logger.LogInformation("{IntroMessage}", NickelConstants.IntroMessage);

		try
		{
			var instance = new Nickel(launchArguments);
			Instance = instance;
			return StartInstance(instance, launchArguments, loggerFactory, logger);
		}
		catch (Exception ex)
		{
			logger.LogCritical("{ModLoaderName} threw an exception: {e}", NickelConstants.Name, ex);
			Instance.ModManager.LogHarmonyPatchesOnce();
			return -3;
		}
	}

	private static int StartInstance(Nickel instance, LaunchArguments launchArguments, ILoggerFactory loggerFactory, ILogger logger)
	{
		var modStorageDirectory = launchArguments.ModStoragePath ?? new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CobaltCore", NickelConstants.Name, "ModStorage"));
		logger.LogInformation("ModStoragePath: {Path}", PathUtilities.SanitizePath(modStorageDirectory.FullName));
		
		var gameLogger = loggerFactory.CreateLogger("CobaltCore");

		ExtendableAssemblyDefinitionEditor extendableAssemblyDefinitionEditor = new(() =>
			new PackageAssemblyResolver(launchArguments.Vanilla ? [] : instance.ModManager.ResolvedMods)
		);
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new NoInliningDefinitionEditor());
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new CobaltCorePublisher());

		Harmony? harmony = null;
		if (!launchArguments.Vanilla)
		{
			harmony = new(NickelConstants.Name);
			instance.Harmony = harmony;
			HarmonyPatches.Apply(harmony, logger);

			var modsDirectory = launchArguments.ModsPath ?? GetOrCreateDefaultModLibraryDirectory();
			logger.LogInformation("ModsPath: {Path}", PathUtilities.SanitizePath(modsDirectory.FullName));

			var privateModStorageDirectory = launchArguments.PrivateModStoragePath ?? new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CobaltCore", NickelConstants.Name, "PrivateModStorage"));
			logger.LogInformation("PrivateModStoragePath: {Path}", PathUtilities.SanitizePath(privateModStorageDirectory.FullName));

			instance.ModManager = new(modsDirectory, modStorageDirectory, privateModStorageDirectory, loggerFactory, logger, extendableAssemblyDefinitionEditor);

			var helper = instance.ModManager.ObtainModHelper(instance.ModManager.ModLoaderPackage);
			instance.Settings = helper.Storage.LoadJson<Settings>(helper.Storage.GetMainStorageFile("json"));
			instance.OnSettingsUpdate();
			
			try
			{
				instance.ModManager.ResolveMods();
			}
			catch (Exception ex)
			{
				logger.LogCritical("{ModLoaderName} threw an exception while resolving mods: {e}", NickelConstants.Name, ex);
				return -4;
			}
			instance.ModManager.LoadMods(ModLoadPhase.BeforeGameAssembly);
		}

		ICobaltCoreResolver cobaltCoreResolver = launchArguments.GamePath is { } gamePath
			? new SingleFileApplicationCobaltCoreResolver(gamePath, new FileInfo(Path.Combine(gamePath.Directory!.FullName, "CobaltCore.pdb")))
			: new SteamCobaltCoreResolver((exe, pdb) => new SingleFileApplicationCobaltCoreResolver(exe, pdb));

		var resolveResultOrError = cobaltCoreResolver.ResolveCobaltCore();
		if (resolveResultOrError.TryPickT1(out var error, out var resolveResult))
		{
			logger.LogCritical("Could not resolve Cobalt Core: {Error}", error.Value);
			return -1;
		}

		var exeBytes = File.ReadAllBytes(resolveResult.ExePath.FullName);
		var hashBytes = MD5.HashData(exeBytes);
		logger.LogDebug("Game EXE hash: {Hash}", Convert.ToHexString(hashBytes));

		var handler = new CobaltCoreHandler(logger, extendableAssemblyDefinitionEditor);
		var handlerResultOrError = handler.SetupGame(resolveResult);
		if (handlerResultOrError.TryPickT1(out error, out var handlerResult))
		{
			logger.LogCritical("Could not start the game: {Error}", error.Value);
			return -2;
		}

		var exitCode = ContinueAfterLoadingGameAssembly(instance, launchArguments, harmony, logger, gameLogger, handlerResult);
		loggerFactory.Dispose();
		return exitCode;
	}

	private static SemanticVersion GetVanillaVersion()
	{
		var vanillaVersionMatch = GameVersionRegex().Match((string)AccessTools.DeclaredField(typeof(CCBuildVars), nameof(CCBuildVars.VERSION)).GetValue(null)!);
		return vanillaVersionMatch.Success
			? new SemanticVersion(
				int.Parse(vanillaVersionMatch.Groups[1].Value),
				int.Parse(vanillaVersionMatch.Groups[2].Value),
				int.Parse(vanillaVersionMatch.Groups[3].Value),
				// the prerelease tag probably won't always match semver, but oh well
				vanillaVersionMatch.Groups.Count >= 5 && !string.IsNullOrEmpty(vanillaVersionMatch.Groups[4].Value)
					? vanillaVersionMatch.Groups[4].Value : null
			)
			: NickelConstants.FallbackGameVersion;
	}

	private static int ContinueAfterLoadingGameAssembly(Nickel instance, LaunchArguments launchArguments, Harmony? harmony, ILogger logger, ILogger gameLogger, CobaltCoreHandlerResult handlerResult)
	{
		if (launchArguments.Debug is { } debugArg)
		{
			if (debugArg)
				instance.DebugMode = (launchArguments.SaveInDebug ?? true) ? DebugMode.EnabledWithSaving : DebugMode.Enabled;
			else
				instance.DebugMode = DebugMode.Disabled;
		}
		logger.LogInformation("DebugMode: {Value}", instance.DebugMode);
		
		var version = GetVanillaVersion();
		logger.LogInformation("Game version: {Version}", version);

		var gameWorkingDirectory = launchArguments.GamePath?.Directory ?? handlerResult.WorkingDirectory;
		logger.LogInformation("GameWorkingDirectory: {Path}", PathUtilities.SanitizePath(gameWorkingDirectory.FullName));

		if (!launchArguments.Vanilla)
		{
			instance.SaveManager = new(
				logger,
				() => instance.ModManager.ContentManager!.Decks,
				() => instance.ModManager.ContentManager!.Statuses
			);

			instance.ModManager.ContinueAfterLoadingGameAssembly(version);
			instance.ModManager.EventManager.OnModLoadPhaseFinishedEvent.Add(instance.OnModLoadPhaseFinished, instance.ModManager.ModLoaderPackage.Manifest);
			instance.ModManager.EventManager.OnLoadStringsForLocaleEvent.Add(instance.OnLoadStringsForLocale, instance.ModManager.ModLoaderPackage.Manifest);

			var savePath = launchArguments.SavePath ?? new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CobaltCore", NickelConstants.Name, "Saves"));
			logger.LogInformation("SavePath: {Path}", PathUtilities.SanitizePath(savePath.FullName));

			if (harmony is not null)
				ApplyHarmonyPatches(harmony);

			LogPatches.OnLine.Subscribe(instance, (_, obj) => gameLogger.LogDebug("{GameLogLine}", obj.ToString()));
			ProgramPatches.OnTryInitSteam.Subscribe(instance, instance.OnTryInitSteam);
			instance.ModManager.LoadMods(ModLoadPhase.AfterGameAssembly);

			FeatureFlags.OverrideSaveLocation = savePath.FullName;
		}
		FeatureFlags.Modded = instance.DebugMode != DebugMode.Disabled || !launchArguments.Vanilla;

		var oldWorkingDirectory = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory(gameWorkingDirectory.FullName);

#pragma warning disable CA2254 // Template should be a static expression
		logger.LogInformation(
			launchArguments.Vanilla
				? "Starting the vanilla game..."
				: "Starting the game..."
		);
#pragma warning restore CA2254 // Template should be a static expression

		try
		{
			List<string> gameArguments = [];
			if (instance.DebugMode != DebugMode.Disabled)
				gameArguments.Add("--debug");
			gameArguments.AddRange(launchArguments.UnmatchedArguments);

			var result = handlerResult.EntryPoint.Invoke(null, BindingFlags.DoNotWrapExceptions, null, [gameArguments.ToArray()], null);
			if (result is not null)
				logger.LogInformation("Cobalt Core closed with result: {Result}", result);
			if (!launchArguments.Vanilla)
				instance.ModManager.EventManager.OnGameClosingEvent.Raise(null, null);
			return 0;
		}
		catch (Exception e)
		{
			logger.LogCritical("Cobalt Core threw an exception: {e}", e);
			if (!launchArguments.Vanilla)
			{
				instance.ModManager.LogHarmonyPatchesOnce();
				instance.ModManager.EventManager.OnGameClosingEvent.Raise(null, e);
			}
			if (instance.LaunchArguments.LogPipeName is null)
				Console.ReadLine();
			return 1;
		}
		finally
		{
			Directory.SetCurrentDirectory(oldWorkingDirectory);
		}
	}

	private static void ApplyHarmonyPatches(Harmony harmony)
	{
		AIPatches.Apply(harmony);
		ArtifactPatches.Apply(harmony);
		ArtifactRewardPatches.Apply(harmony);
		CardPatches.Apply(harmony);
		CheevosPatches.Apply(harmony);
		CombatPatches.Apply(harmony);
		DBPatches.Apply(harmony);
		EventsPatches.Apply(harmony);
		GPatches.Apply(harmony);
		LogPatches.Apply(harmony);
		ProgramPatches.Apply(harmony);
		RunSummaryPatches.Apply(harmony);
		SpriteLoaderPatches.Apply(harmony);
		StatePatches.Apply(harmony);
		StoryVarsPatches.Apply(harmony);
		TTGlossaryPatches.Apply(harmony);
		WizardPatches.Apply(harmony);

		GenericKeyPatches.Apply<CardAction>(harmony);
		GenericKeyPatches.Apply<FightModifier>(harmony);
		GenericKeyPatches.Apply<MapBase>(harmony);
	}

	internal static void ApplyLateHarmonyPatches(Harmony harmony)
		=> MapBasePatches.ApplyLate(harmony);

	[EventPriority(double.PositiveInfinity)]
	private void OnModLoadPhaseFinished(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterDbInit)
			return;

		if (this.Harmony is not null)
			ApplyLateHarmonyPatches(this.Harmony);
		this.ModManager.LogHarmonyPatchesOnce();
		this.ModManager.ContentManager?.InjectQueuedEntries();

		this.SetupModSettings();
	}

	private void SetupModSettings()
	{
		var helper = this.ModManager.ObtainModHelper(this.ModManager.ModLoaderPackage);
		if (helper.ModRegistry.GetApi<IModSettingsApi>("Nickel.ModSettings") is { } settingsApi)
			settingsApi.RegisterModSettings(settingsApi.MakeList([
				settingsApi.MakeConditional(
					setting: settingsApi.MakeEnumStepper(
							title: () => "Debug", // TODO: localize
							getter: () => this.Settings.DebugMode,
							setter: value =>
							{
								this.Settings.DebugMode = value;
								this.OnSettingsUpdate();
							}
						).SetValueFormatter(
							mode => mode switch
							{
								DebugMode.Disabled => "Off",
								DebugMode.Enabled => "On",
								DebugMode.EnabledWithSaving => "On w/ auto-save",
								_ => mode.ToString()
							}
						)
						.SetValueWidth(
							_ => 120
						),
					isVisible: () => Instance.LaunchArguments.Debug is null
				),
				settingsApi.MakeConditional(
					setting: settingsApi.MakeButton(
						title: () => "Toggle debug menu",
						(g, _) =>
						{
							Audio.Play(Event.Click);
							if (g.e is { } editor)
								editor.isActive = !editor.isActive;
						}
					),
					isVisible: () => FeatureFlags.Debug
				)
			]).SubscribeToOnMenuClose(_ =>
			{
				helper.Storage.SaveJson(helper.Storage.GetMainStorageFile("json"), this.Settings);
				this.OnSettingsUpdate();
			}));
	}

	[EventPriority(double.MaxValue)]
	private void OnLoadStringsForLocale(object? _, LoadStringsForLocaleEventArgs e)
		=> this.ModManager.ContentManager?.InjectLocalizations(e.Locale, e.Localizations);

	private void OnTryInitSteam(object? _, StructRef<bool> initSteam)
		=> initSteam.Value = this.LaunchArguments.InitSteam ?? true;

	private static DirectoryInfo GetOrCreateDefaultModLibraryDirectory()
	{
		var directoryInfo = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModLibrary"));
		if (!directoryInfo.Exists)
			directoryInfo.Create();
		return directoryInfo;
	}

	private static DirectoryInfo GetOrCreateDefaultLogDirectory()
	{
		var directoryInfo = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
		if (!directoryInfo.Exists)
			directoryInfo.Create();
		return directoryInfo;
	}

	private void OnSettingsUpdate()
	{
		this.DebugMode = this.Settings.DebugMode;
		if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "CobaltCore"))
			this.OnSettingsUpdateAfterGameLoaded();
	}

	private void OnSettingsUpdateAfterGameLoaded()
	{
		FeatureFlags.Debug = this.DebugMode != DebugMode.Disabled;

		// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
		if (MG.inst?.g is not { } g)
			return;

		if (g.e is null)
		{
			g.e = new Editor();
			g.e.IMGUI_Setup(MG.inst);
		}
	}

	[GeneratedRegex(@"(\d+)\.(\d+)\.(\d+)(?: (.+))?")]
	private static partial Regex GameVersionRegex();
}
