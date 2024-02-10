using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager.Cecil;
using Nanoray.Shrike;
using Nickel.Common;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Nickel;

internal sealed partial class Nickel(LaunchArguments launchArguments)
{
	internal static Nickel Instance { get; private set; } = null!;
	internal ModManager ModManager { get; private set; } = null!;
	internal LaunchArguments LaunchArguments { get; } = launchArguments;

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
		rootCommand.AddOption(savePathOption);
		rootCommand.AddOption(logPathOption);
		rootCommand.AddOption(timestampedLogFiles);
		rootCommand.AddOption(logPipeNameOption);

		rootCommand.SetHandler((InvocationContext context) =>
		{
			LaunchArguments launchArguments = new()
			{
				Vanilla = context.ParseResult.GetValueForOption(vanillaOption),
				Debug = context.ParseResult.GetValueForOption(debugOption),
				SaveInDebug = context.ParseResult.GetValueForOption(saveInDebugOption),
				InitSteam = context.ParseResult.GetValueForOption(initSteamOption),
				GamePath = context.ParseResult.GetValueForOption(gamePathOption),
				ModsPath = context.ParseResult.GetValueForOption(modsPathOption),
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
			return CreateAndStartInstance(launchArguments, loggerFactory, logger);
		}
		catch (Exception ex)
		{
			logger.LogCritical("{ModLoaderName} threw an exception: {e}", NickelConstants.Name, ex);
			Instance?.ModManager.LogHarmonyPatchesOnce();
			return -3;
		}
	}

	private static int CreateAndStartInstance(LaunchArguments launchArguments, ILoggerFactory loggerFactory, ILogger logger)
	{
		var gameLogger = loggerFactory.CreateLogger("CobaltCore");
		var instance = new Nickel(launchArguments);
		Instance = instance;

		ExtendableAssemblyDefinitionEditor extendableAssemblyDefinitionEditor = new(() =>
			new PackageAssemblyResolver(launchArguments.Vanilla ? [] : instance.ModManager.ResolvedMods)
		);
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new NoInliningDefinitionEditor());
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new CobaltCorePublisher());

		Harmony? harmony = null;
		if (!launchArguments.Vanilla)
		{
			harmony = new(NickelConstants.Name);
			HarmonyPatches.Apply(harmony, logger);

			var modsDirectory = launchArguments.ModsPath ?? GetOrCreateDefaultModLibraryDirectory();
			logger.LogInformation("ModsPath: {Path}", modsDirectory.FullName);

			instance.ModManager = new(modsDirectory, loggerFactory, logger, extendableAssemblyDefinitionEditor);
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
		var version = GetVanillaVersion();
		logger.LogInformation("Game version: {Version}", version);

		var debug = launchArguments.Debug ?? !launchArguments.Vanilla;
		logger.LogInformation("Debug: {Value}", debug);

		var gameWorkingDirectory = launchArguments.GamePath?.Directory ?? handlerResult.WorkingDirectory;
		logger.LogInformation("GameWorkingDirectory: {Path}", gameWorkingDirectory.FullName);

		if (!launchArguments.Vanilla)
		{
			instance.ModManager.ContinueAfterLoadingGameAssembly(version);
			instance.ModManager.EventManager.OnModLoadPhaseFinishedEvent.Add(instance.OnModLoadPhaseFinished, instance.ModManager.ModLoaderModManifest);
			instance.ModManager.EventManager.OnLoadStringsForLocaleEvent.Add(instance.OnLoadStringsForLocale, instance.ModManager.ModLoaderModManifest);

			var saveInDebug = launchArguments.SaveInDebug ?? true;
			logger.LogInformation("SaveInDebug: {Value}", saveInDebug);

			var savePath = launchArguments.SavePath?.FullName ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModSaves");
			logger.LogInformation("SavePath: {Path}", savePath);

			if (harmony is not null)
				ApplyHarmonyPatches(harmony, saveInDebug);

			LogPatches.OnLine.Subscribe(instance, (_, obj) => gameLogger.LogDebug("{GameLogLine}", obj.ToString()));
			ProgramPatches.OnTryInitSteam.Subscribe(instance, instance.OnTryInitSteam);
			instance.ModManager.LoadMods(ModLoadPhase.AfterGameAssembly);

			FeatureFlags.OverrideSaveLocation = savePath;
		}
		FeatureFlags.Modded = debug || !launchArguments.Vanilla;

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
			List<string> gameArguments = new();
			if (debug)
				gameArguments.Add("--debug");
			gameArguments.AddRange(launchArguments.UnmatchedArguments);

			var result = handlerResult.EntryPoint.Invoke(null, BindingFlags.DoNotWrapExceptions, null, [gameArguments.ToArray()], null);
			if (result is not null)
				logger.LogInformation("Cobalt Core closed with result: {Result}", result);
			return 0;
		}
		catch (Exception e)
		{
			logger.LogCritical("Cobalt Core threw an exception: {e}", e);
			if (!launchArguments.Vanilla)
				instance.ModManager.LogHarmonyPatchesOnce();
			if (instance.LaunchArguments.LogPipeName is null)
				Console.ReadLine();
			return 1;
		}
		finally
		{
			Directory.SetCurrentDirectory(oldWorkingDirectory);
		}
	}

	private static void ApplyHarmonyPatches(Harmony harmony, bool saveInDebug)
	{
		ArtifactPatches.Apply(harmony);
		ArtifactRewardPatches.Apply(harmony);
		CardPatches.Apply(harmony);
		DBPatches.Apply(harmony);
		EventsPatches.Apply(harmony);
		GPatches.Apply(harmony);
		LogPatches.Apply(harmony);
		ProgramPatches.Apply(harmony);
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

	[GeneratedRegex("(\\d+)\\.(\\d+)\\.(\\d+)(?: (.+))?")]
	private static partial Regex GameVersionRegex();
}
