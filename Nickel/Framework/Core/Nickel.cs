using HarmonyLib;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Nickel;

internal sealed partial class Nickel(ProgramRunInfo info)
{
	internal static Nickel Instance { get; private set; } = null!;
	internal Harmony? Harmony { get; private set; }
	internal ModManager ModManager { get; private set; } = null!;
	internal readonly ProgramRunInfo RunInfo = info;
	
	private SaveManager SaveManager = null!;

	internal static bool Run(ProgramRunInfo info, ILoggerFactory? loggerFactory = null)
	{
		var stopwatch = Stopwatch.StartNew();
		var realOut = Console.Out;
		loggerFactory ??= LoggerFactory.Create(builder =>
		{
			var logPipeName = info.LaunchArgs.GetValueForOption(LaunchOptions.LogPipeName);
			if (string.IsNullOrEmpty(logPipeName))
			{
				Program.SetupDefaultLogger(builder, info, realOut);
				return;
			}
			
			builder.SetMinimumLevel((LogLevel)Math.Min((int)info.Settings.Logging.MinimumFileLogLevel, (int)info.Settings.Logging.MinimumConsoleLogLevel));
			builder.AddProvider(new NamedPipeClientLoggerProvider(logPipeName));
		});
		var logger = loggerFactory.CreateLogger(NickelConstants.Name);
		Console.SetOut(new LoggerTextWriter(logger, LogLevel.Information, realOut));
		Console.SetError(new LoggerTextWriter(logger, LogLevel.Error, Console.Error));
		logger.LogInformation("{IntroMessage}", NickelConstants.IntroMessage);
		
		logger.LogInformation("ModStoragePath: {Path}", info.ModStorageDirectory.FullName);
		info.PushEarlyLogsToLogger(logger);

		try
		{
			logger.LogInformation("DebugMode: {Value}", info.Settings.DebugMode);
			
			var instance = new Nickel(info);
			Instance = instance;
			return StartInstance(instance, loggerFactory, logger, stopwatch);
		}
		catch (Exception ex)
		{
			logger.LogCritical("{ModLoaderName} threw an exception: {e}", NickelConstants.Name, ex);
			Instance?.ModManager.LogHarmonyPatchesOnce();
			return false;
		}
	}

	private static bool StartInstance(Nickel instance, ILoggerFactory loggerFactory, ILogger logger, Stopwatch stopwatch)
	{
		var steamCompatDataPath = Environment.GetEnvironmentVariable("STEAM_COMPAT_DATA_PATH");
		if (!string.IsNullOrEmpty(steamCompatDataPath))
			logger.LogInformation("SteamCompatDataPath: {Path}", steamCompatDataPath);
		
		ICobaltCoreResolver cobaltCoreResolver = instance.RunInfo.Settings.GamePath is { } gamePath
			? new SingleFileApplicationCobaltCoreResolver(
				new FileInfoImpl(gamePath),
				new FileInfoImpl(new FileInfo(Path.Combine(gamePath.Directory!.FullName, "CobaltCore.pdb"))),
				logger
			)
			: new CompoundCobaltCoreResolver([
				new RecursiveToRootDirectoryCobaltCoreResolver(
					new DirectoryInfoImpl(new DirectoryInfo(Environment.CurrentDirectory)),
					directory =>
					{
						var exePath = directory.GetRelativeFile(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "CobaltCore.exe" : "CobaltCore");
						var pdbPath = directory.GetRelativeFile("CobaltCore.pdb");
						return exePath is { Exists: true, IsFile: true }
							? new SingleFileApplicationCobaltCoreResolver(exePath, pdbPath, logger)
							: null;
					},
					logger
				),
				new SteamCobaltCoreResolver(
					(exePath, pdbPath) => new SingleFileApplicationCobaltCoreResolver(exePath, pdbPath, logger),
					logger
				),
			]);

		var resolveResultOrError = cobaltCoreResolver.ResolveCobaltCore();
		if (resolveResultOrError.TryPickT1(out var resolveError, out var resolveResult))
		{
			logger.LogCritical("Could not resolve Cobalt Core: {Error}", resolveError.Value);
			return false;
		}
		
		logger.LogDebug("Resolved game EXE path: {Path}", resolveResult.ExePath.FullName);
		logger.LogDebug("Resolved game working directory path: {Path}", resolveResult.WorkingDirectory.FullName);
		
		using (var exeStream = resolveResult.ExePath.OpenRead())
			logger.LogDebug("Game EXE hash: {Hash}", Convert.ToHexString(MD5.HashData(exeStream)));

		var extendableAssemblyDefinitionEditor = new ExtendableAssemblyDefinitionEditor(() => new CompoundAssemblyResolver([
			new CobaltCoreAssemblyResolver(resolveResult),
			new PackageAssemblyResolver(instance.ModManager.ResolvedMods),
			new DefaultAssemblyResolver(),
		]));
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new NoInliningDefinitionEditor(
			() => instance.ModManager.ModLoaderPackage.Manifest,
			() => instance.ModManager.ResolvedMods
				.Select(p => p.Manifest.AsAssemblyModManifest())
				.Where(m => m.IsT0)
				.Select(m => m.AsT0)
		));
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new GamePublicizerDefinitionEditor());
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new CardDataExtraTraitsFieldDefinitionEditor());
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new CardTraitStateCacheFieldDefinitionEditor());
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new ModDataFieldDefinitionEditor());
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new DeepCopyViaMitosisDefinitionEditor());
		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new GameFieldToPropertyDefinitionEditor());

		var assemblyCacheDirectory = instance.RunInfo.Settings.AssemblyCachePath ?? GetOrCreateDefaultAssemblyCacheDirectory();
		logger.LogInformation("AssemblyCachePath: {Path}", assemblyCacheDirectory.FullName);

		var fileCachingAssemblyEditor = new FileCachingAssemblyEditor(
			extendableAssemblyDefinitionEditor,
			new DirectoryInfoImpl(assemblyCacheDirectory)
		);
		fileCachingAssemblyEditor.ReadEntries();

		AppDomain.CurrentDomain.ProcessExit += (_, _) =>
		{
			try
			{
				fileCachingAssemblyEditor.CleanupEntries();
			}
			catch (Exception ex)
			{
				logger.LogError("Error while cleaning up cached assemblies: {Exception}", ex);
			}
			
			try
			{
				fileCachingAssemblyEditor.WriteEntries();
			}
			catch (Exception ex)
			{
				logger.LogError("Error while writing cached assembly entries: {Exception}", ex);
			}
		};

		var harmony = new Harmony(NickelConstants.Name);
		instance.Harmony = harmony;
		HarmonyPatches.Apply(harmony, logger);

		var internalModsDirectory = instance.RunInfo.Settings.InternalModsPath ?? GetOrCreateDefaultInternalModLibraryDirectory();
		logger.LogInformation("InternalModsPath: {Path}", internalModsDirectory.FullName);

		var modsDirectory = instance.RunInfo.Settings.ModsPath ?? GetOrCreateDefaultModLibraryDirectory();
		logger.LogInformation("ModsPath: {Path}", modsDirectory.FullName);

		var privateModStorageDirectory = instance.RunInfo.Settings.PrivateModStoragePath ?? new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CobaltCore", NickelConstants.Name, "PrivateModStorage"));
		logger.LogInformation("PrivateModStoragePath: {Path}", privateModStorageDirectory.FullName);

		instance.ModManager = new(
			internalModsDirectory,
			modsDirectory,
			instance.RunInfo.ModStorageDirectory,
			privateModStorageDirectory,
			loggerFactory,
			logger,
			fileCachingAssemblyEditor,
			extendableAssemblyDefinitionEditor,
			stopwatch,
			instance.RunInfo.Settings.AttachDebuggerBeforeMod,
			instance.RunInfo.Settings.AttachDebuggerAfterMod,
			instance.RunInfo.Settings.AttachDebuggerBeforeModLoadPhase,
			instance.RunInfo.Settings.AttachDebuggerAfterModLoadPhase
		);
		
		try
		{
			instance.ModManager.ResolveMods();
		}
		catch (Exception ex)
		{
			logger.LogCritical("{ModLoaderName} threw an exception while resolving mods: {e}", NickelConstants.Name, ex);
			return false;
		}
		instance.ModManager.LoadMods(ModLoadPhase.BeforeGameAssembly);

		var handler = new CobaltCoreHandler(logger, extendableAssemblyDefinitionEditor);
		var handlerResultOrError = handler.SetupGame(resolveResult);
		if (handlerResultOrError.TryPickT1(out var handlerError, out var handlerResult))
		{
			logger.LogCritical("Could not start the game: {Error}", handlerError.Value);
			return false;
		}
		
		var gameLogger = loggerFactory.CreateLogger("CobaltCore");
		var success = ContinueAfterLoadingGameAssembly(instance, harmony, logger, gameLogger, handlerResult);
		loggerFactory.Dispose();
		return success;
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

	private static bool ContinueAfterLoadingGameAssembly(Nickel instance, Harmony? harmony, ILogger logger, ILogger gameLogger, CobaltCoreHandlerResult handlerResult)
	{
		var version = GetVanillaVersion();
		logger.LogInformation("Game version: {Version}", version);

		if (NickelConstants.MinimumGameVersion is { } minimumGameVersion && version < minimumGameVersion)
		{
			logger.LogCritical("{ModLoaderName}'s minimum supported game version is {MinimumGameVersion}, but the game is at version {GameVersion}; aborting.", NickelConstants.Name, NickelConstants.MinimumGameVersion, version);
			return false;
		}

		instance.SaveManager = new(
			logger,
			() => instance.ModManager.ContentManager!.Decks,
			() => instance.ModManager.ContentManager!.Statuses
		);

		instance.ModManager.ContinueAfterLoadingGameAssembly(version);
		instance.ModManager.EventManager.OnModLoadPhaseFinishedEvent.Add(instance.OnModLoadPhaseFinished, instance.ModManager.ModLoaderPackage.Manifest);
		instance.ModManager.EventManager.OnLoadStringsForLocaleEvent.Add(instance.OnLoadStringsForLocale, instance.ModManager.ModLoaderPackage.Manifest);

		var savePath = instance.RunInfo.Settings.SavePath ?? new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CobaltCore", NickelConstants.Name, "Saves"));
		logger.LogInformation("SavePath: {Path}", savePath.FullName);

		if (harmony is not null)
			ApplyHarmonyPatches(harmony);

		LogPatches.OnLine += (_, obj) => gameLogger.LogDebug("{GameLogLine}", obj.ToString());
		ProgramPatches.OnTryInitSteam += OnTryInitSteam;
		instance.ModManager.EventManager.SetupAfterGameAssembly();
		instance.ModManager.LoadMods(ModLoadPhase.AfterGameAssembly);

		FeatureFlags.OverrideSaveLocation = savePath.FullName;
		FeatureFlags.Modded = true;

		var oldWorkingDirectory = Directory.GetCurrentDirectory();
		var gameWorkingDirectory = handlerResult.WorkingDirectory;
		Directory.SetCurrentDirectory(gameWorkingDirectory.FullName);
		
		// Steam fails to init otherwise on Mac
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			File.WriteAllBytes("steam_appid.txt", Encoding.UTF8.GetBytes(NickelConstants.GameSteamAppId));

		logger.LogInformation("Starting the game...");

		try
		{
			List<string> gameArguments = [];
			if (instance.RunInfo.Settings.DebugMode != DebugMode.Disabled)
				gameArguments.Add("--debug");
			// gameArguments.AddRange(launchArguments.UnmatchedArguments);

			var result = handlerResult.EntryPoint.Invoke(null, BindingFlags.DoNotWrapExceptions, null, [gameArguments.ToArray()], null);
			if (result is not null)
				logger.LogInformation("Cobalt Core closed with result: {Result}", result);
			instance.ModManager.EventManager.OnGameClosingEvent.Raise(null, null);
			return true;
		}
		catch (Exception e)
		{
			logger.LogCritical("Cobalt Core threw an exception: {e}", e);
			instance.ModManager.LogHarmonyPatchesOnce();
			instance.ModManager.EventManager.OnGameClosingEvent.Raise(null, e);
			if (instance.RunInfo.LaunchArgs.GetValueForOption(LaunchOptions.LogPipeName) is null)
				Console.ReadLine();
			return false;
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
		AudioPatches.Apply(harmony);
		BigStatsPatches.Apply(harmony);
		CardPatches.Apply(harmony);
		CheevosPatches.Apply(harmony);
		CombatPatches.Apply(harmony);
		DBPatches.Apply(harmony);
		EventsPatches.Apply(harmony);
		GPatches.Apply(harmony);
		LogPatches.Apply(harmony);
		MGPatches.Apply(harmony);
		ProgramPatches.Apply(harmony);
		RunSummaryPatches.Apply(harmony);
		ShipPatches.Apply(harmony);
		ShoutPatches.Apply(harmony);
		SpriteLoaderPatches.Apply(harmony);
		StatePatches.Apply(harmony);
		StoryVarsPatches.Apply(harmony);
		TTGlossaryPatches.Apply(harmony);
		WizardPatches.Apply(harmony);

		GenericKeyPatches.Apply<CardAction>(harmony);
		GenericKeyPatches.Apply<FightModifier>(harmony);
		GenericKeyPatches.Apply<MapBase>(harmony);
	}

	private static void ApplyLateHarmonyPatches(Harmony harmony)
		=> MapBasePatches.ApplyLate(harmony);

	[EventPriority(double.PositiveInfinity)]
	private void OnModLoadPhaseFinished(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterDbInit)
			return;

		if (this.Harmony is not null)
			ApplyLateHarmonyPatches(this.Harmony);
		this.ModManager.ContentManager?.InjectQueuedEntries();
	}

	[EventPriority(double.MaxValue)]
	private void OnLoadStringsForLocale(object? _, LoadStringsForLocaleEventArgs e)
		=> this.ModManager.ContentManager?.InjectLocalizations(e.Locale, e.Localizations);

	private static void OnTryInitSteam(object? _, ref bool initSteam)
		=> initSteam = Instance.RunInfo.Settings.InitSteam;

	private static DirectoryInfo GetOrCreateDefaultInternalModLibraryDirectory()
	{
		var directoryInfo = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InternalModLibrary"));
		if (!directoryInfo.Exists)
			directoryInfo.Create();
		return directoryInfo;
	}

	private static DirectoryInfo GetOrCreateDefaultModLibraryDirectory()
	{
		DirectoryInfo directoryInfo;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			directoryInfo = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), NickelConstants.Name, "ModLibrary"));
		else
			directoryInfo = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModLibrary"));
		
		if (!directoryInfo.Exists)
			directoryInfo.Create();
		return directoryInfo;
	}

	private static DirectoryInfo GetOrCreateDefaultAssemblyCacheDirectory()
	{
		DirectoryInfo directoryInfo;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			directoryInfo = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), NickelConstants.Name, "AssemblyCache"));
		else
			directoryInfo = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AssemblyCache"));
		
		if (!directoryInfo.Exists)
			directoryInfo.Create();
		return directoryInfo;
	}

	[GeneratedRegex(@"(\d+)\.(\d+)\.(\d+)(?: (.+))?")]
	private static partial Regex GameVersionRegex();
}
