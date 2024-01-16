using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager.Cecil;
using Nickel.Common;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Nickel;

internal sealed partial class Nickel
{
	internal static Nickel Instance { get; private set; } = null!;
	internal ModManager ModManager { get; private set; } = null!;

	internal static int Main(string[] args)
	{
		Option<bool?> debugOption = new(
			name: "--debug",
			description: "Whether the game should be ran in debug mode."
		);
		Option<bool?> saveInDebugOption = new(
			name: "--saveInDebug",
			description: "Whether the game should be auto-saved even in debug mode."
		);
		Option<FileInfo?> gamePathOption = new(
			name: "--gamePath",
			description: "The path to CobaltCore.exe."
		);
		Option<DirectoryInfo?> modsPathOption = new(
			name: "--modsPath",
			description: "The path containing the mods to load."
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
		rootCommand.AddOption(debugOption);
		rootCommand.AddOption(saveInDebugOption);
		rootCommand.AddOption(gamePathOption);
		rootCommand.AddOption(modsPathOption);
		rootCommand.AddOption(savePathOption);
		rootCommand.AddOption(logPathOption);
		rootCommand.AddOption(timestampedLogFiles);
		rootCommand.AddOption(logPipeNameOption);

		rootCommand.SetHandler((InvocationContext context) =>
		{
			LaunchArguments launchArguments = new()
			{
				Debug = context.ParseResult.GetValueForOption(debugOption),
				SaveInDebug = context.ParseResult.GetValueForOption(saveInDebugOption),
				GamePath = context.ParseResult.GetValueForOption(gamePathOption),
				ModsPath = context.ParseResult.GetValueForOption(modsPathOption),
				SavePath = context.ParseResult.GetValueForOption(savePathOption),
				LogPath = context.ParseResult.GetValueForOption(logPathOption),
				TimestampedLogFiles = context.ParseResult.GetValueForOption(timestampedLogFiles),
				LogPipeName = context.ParseResult.GetValueForOption(logPipeNameOption),
				UnmatchedArguments = context.ParseResult.UnmatchedTokens
			};
			CreateAndStartInstance(launchArguments);
		});
		return rootCommand.Invoke(args);
	}

	private static void CreateAndStartInstance(LaunchArguments launchArguments)
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

		Nickel instance = new();
		Instance = instance;

		Harmony harmony = new(NickelConstants.Name);
		HarmonyPatches.Apply(harmony, logger);

		var modsDirectory = launchArguments.ModsPath ?? GetOrCreateDefaultModLibraryDirectory();
		logger.LogInformation("ModsPath: {Path}", modsDirectory.FullName);

		ExtendableAssemblyDefinitionEditor extendableAssemblyDefinitionEditor = new(() =>
			new PackageAssemblyResolver(instance.ModManager.ResolvedMods)
		);
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new NoInliningDefinitionEditor());
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new CobaltCorePublisher());
		instance.ModManager = new(modsDirectory, loggerFactory, logger, extendableAssemblyDefinitionEditor);
		instance.ModManager.ResolveMods();
		instance.ModManager.LoadMods(ModLoadPhase.BeforeGameAssembly);

		ICobaltCoreResolver cobaltCoreResolver = launchArguments.GamePath is { } gamePath
			? new SingleFileApplicationCobaltCoreResolver(gamePath, new FileInfo(Path.Combine(gamePath.Directory!.FullName, "CobaltCore.pdb")))
			: new SteamCobaltCoreResolver((exe, pdb) => new SingleFileApplicationCobaltCoreResolver(exe, pdb));

		var resolveResultOrError = cobaltCoreResolver.ResolveCobaltCore();
		if (resolveResultOrError.TryPickT1(out var error, out var resolveResult))
		{
			logger.LogCritical("Could not resolve Cobalt Core: {Error}", error.Value);
			return;
		}

		var exeBytes = File.ReadAllBytes(resolveResult.ExePath.FullName);
		var hashBytes = MD5.HashData(exeBytes);
		logger.LogDebug("Game EXE hash: {Hash}", Convert.ToHexString(hashBytes));

		var handler = new CobaltCoreHandler(logger, extendableAssemblyDefinitionEditor);
		var handlerResultOrError = handler.SetupGame(resolveResult);
		if (handlerResultOrError.TryPickT1(out error, out var handlerResult))
		{
			logger.LogCritical("Could not start the game: {Error}", error.Value);
			return;
		}

		ContinueAfterLoadingGameAssembly(instance, launchArguments, harmony, logger, handlerResult);
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

	private static void ContinueAfterLoadingGameAssembly(Nickel instance, LaunchArguments launchArguments, Harmony harmony, ILogger logger, CobaltCoreHandlerResult handlerResult)
	{
		var version = GetVanillaVersion();
		logger.LogInformation("Game version: {Version}", version);

		instance.ModManager.ContinueAfterLoadingGameAssembly(version);
		instance.ModManager.EventManager.OnModLoadPhaseFinishedEvent.Add(instance.OnModLoadPhaseFinished, instance.ModManager.ModLoaderModManifest);
		instance.ModManager.EventManager.OnLoadStringsForLocaleEvent.Add(instance.OnLoadStringsForLocale, instance.ModManager.ModLoaderModManifest);

		var debug = launchArguments.Debug ?? true;
		logger.LogInformation("Debug: {Value}", debug);

		var saveInDebug = launchArguments.SaveInDebug ?? true;
		logger.LogInformation("SaveInDebug: {Value}", saveInDebug);

		var gameWorkingDirectory = launchArguments.GamePath?.Directory ?? handlerResult.WorkingDirectory;
		logger.LogInformation("GameWorkingDirectory: {Path}", gameWorkingDirectory.FullName);

		var savePath = launchArguments.SavePath?.FullName ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModSaves");
		logger.LogInformation("SavePath: {Path}", savePath);

		ApplyHarmonyPatches(harmony, saveInDebug);
		instance.ModManager.LoadMods(ModLoadPhase.AfterGameAssembly);

		var oldWorkingDirectory = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory(gameWorkingDirectory.FullName);

		logger.LogInformation("Starting the game...");
		FeatureFlags.Modded = true;
		FeatureFlags.OverrideSaveLocation = savePath;
		try
		{
			List<string> gameArguments = new();
			if (debug)
				gameArguments.Add("--debug");
			gameArguments.AddRange(launchArguments.UnmatchedArguments);

			var result = handlerResult.EntryPoint.Invoke(null, System.Reflection.BindingFlags.DoNotWrapExceptions, null, [gameArguments.ToArray()], null);
			if (result is not null)
				logger.LogInformation("Cobalt Core closed with result: {Result}", result);
		}
		catch (Exception e)
		{
			logger.LogCritical("Cobalt Core threw an exception: {e}", e);
			instance.ModManager.LogHarmonyPatchesOnce();
		}
		Directory.SetCurrentDirectory(oldWorkingDirectory);
	}

	private static void ApplyHarmonyPatches(Harmony harmony, bool saveInDebug)
	{
		ArtifactPatches.Apply(harmony);
		ArtifactRewardPatches.Apply(harmony);
		CardPatches.Apply(harmony);
		DBPatches.Apply(harmony);
		GPatches.Apply(harmony);
		RunSummaryPatches.Apply(harmony);
		SpriteLoaderPatches.Apply(harmony);
		StatePatches.Apply(harmony, saveInDebug);
		StoryVarsPatches.Apply(harmony);
		TTGlossaryPatches.Apply(harmony);
		WizardPatches.Apply(harmony);

		GenericKeyPatches.Apply<CardAction>(harmony);
		GenericKeyPatches.Apply<FightModifier>(harmony);
		GenericKeyPatches.Apply<MapBase>(harmony);
		GenericKeyPatches.Apply<AI>(harmony);
	}

	[EventPriority(double.MaxValue)]
	private void OnModLoadPhaseFinished(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterDbInit)
			return;
		this.ModManager.ContentManager?.InjectQueuedEntries();
	}

	[EventPriority(double.MaxValue)]
	private void OnLoadStringsForLocale(object? _, LoadStringsForLocaleEventArgs e)
		=> this.ModManager.ContentManager?.InjectLocalizations(e.Locale, e.Localizations);

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

	[GeneratedRegex("(\\d+)\\.(\\d+)\\.(\\d+)(?: (.+))?")]
	private static partial Regex GameVersionRegex();
}
