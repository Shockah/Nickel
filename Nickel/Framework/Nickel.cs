using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Nanoray.PluginManager.Cecil;
using Nickel.Common;
using Nickel.Framework.Utilities;

namespace Nickel;

internal sealed class Nickel
{
    internal static Nickel Instance { get; private set; } = null!;
    internal ModManager ModManager { get; private set; } = null!;

    internal static int Main(string[] args)
    {
        Option<bool?> debugOption = new(
            name: "--debug",
            description: "Whether the game should be ran in debug mode."
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
        Option<string?> logPipeNameOption = new(
            name: "--logPipeName"
        ) { IsHidden = true };

        RootCommand rootCommand = new(NickelConstants.IntroMessage);
        rootCommand.AddOption(debugOption);
        rootCommand.AddOption(gamePathOption);
        rootCommand.AddOption(modsPathOption);
        rootCommand.AddOption(savePathOption);
        rootCommand.AddOption(logPipeNameOption);

        rootCommand.SetHandler((InvocationContext context) =>
        {
            LaunchArguments launchArguments = new()
            {
                Debug = context.ParseResult.GetValueForOption(debugOption),
                GamePath = context.ParseResult.GetValueForOption(gamePathOption),
                ModsPath = context.ParseResult.GetValueForOption(modsPathOption),
                SavePath = context.ParseResult.GetValueForOption(savePathOption),
                LogPipeName = context.ParseResult.GetValueForOption(logPipeNameOption),
                UnmatchedArguments = context.ParseResult.UnmatchedTokens
            };
            CreateAndStartInstance(launchArguments);
        });
        return rootCommand.Invoke(args);
    }

    private static void CreateAndStartInstance(LaunchArguments launchArguments)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConfiguration();

            if (string.IsNullOrEmpty(launchArguments.LogPipeName))
            {
                builder.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
                builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleLoggerProvider>());
                LoggerProviderOptions.RegisterProviderOptions<ConsoleLoggerOptions, ConsoleLoggerProvider>(builder.Services);
            }
            else
            {
                builder.AddProvider(new NamedPipeClientLoggerProvider(launchArguments.LogPipeName));
            }
        });
        var logger = loggerFactory.CreateLogger(NickelConstants.Name);
        Console.SetOut(new LoggerTextWriter(logger, LogLevel.Information, Console.Out));
        Console.SetError(new LoggerTextWriter(logger, LogLevel.Error, Console.Error));
        logger.LogInformation("{IntroMessage}", NickelConstants.IntroMessage);

        Nickel instance = new();
        Instance = instance;

        Harmony harmony = new(NickelConstants.Name);
        HarmonyPatches.Apply(harmony, logger);

        var modsDirectory = launchArguments.ModsPath ?? GetOrCreateDefaultModLibraryDirectory();
        logger.LogInformation("ModsPath: {Path}", modsDirectory.FullName);

        ExtendableAssemblyDefinitionEditor extendableAssemblyDefinitionEditor = new();
        instance.ModManager = new(modsDirectory, loggerFactory, logger, extendableAssemblyDefinitionEditor);
        instance.ModManager.ResolveMods();
        instance.ModManager.LoadMods(ModLoadPhase.BeforeGameAssembly);

        CobaltCoreHandler handler = new(
            logger,
            launchArguments.GamePath is { } gamePath
                ? new SingleFileApplicationCobaltCoreResolver(gamePath, new FileInfo(Path.Combine(gamePath.Directory!.FullName, "CobaltCore.pdb")))
                : new SteamCobaltCoreResolver((exe, pdb) => new SingleFileApplicationCobaltCoreResolver(exe, pdb)),
            extendableAssemblyDefinitionEditor
        );

        var handlerResultOrError = handler.SetupGame();
        if (handlerResultOrError.TryPickT1(out var error, out var handlerResult))
        {
            logger.LogCritical("Could not start the game: {Error}", error.Value);
            return;
        }

        ContinueAfterLoadingGameAssembly(instance, launchArguments, harmony, logger, handlerResult);
    }

    private static void ContinueAfterLoadingGameAssembly(Nickel instance, LaunchArguments launchArguments, Harmony harmony, ILogger logger, CobaltCoreHandlerResult handlerResult)
    {
        instance.ModManager.ContinueAfterLoadingGameAssembly(handlerResult.GameAssembly);
        instance.ModManager.EventManager.OnModLoadPhaseFinishedEvent.Add(instance.OnModLoadPhaseFinished, instance.ModManager.ModLoaderModManifest);
        instance.ModManager.EventManager.OnLoadStringsForLocaleEvent.Add(instance.OnLoadStringsForLocale, instance.ModManager.ModLoaderModManifest);

        DBPatches.Apply(harmony, logger);
        MGPatches.Apply(harmony, logger);
        SpriteLoaderPatches.Apply(harmony, logger);
        StoryVarsPatches.Apply(harmony, logger);
        TTGlossaryPatches.Apply(harmony, logger);

        bool debug = launchArguments.Debug ?? true;
        logger.LogInformation("Debug: {Value}", debug);

        var gameWorkingDirectory = launchArguments.GamePath?.Directory ?? handlerResult.WorkingDirectory;
        logger.LogInformation("GameWorkingDirectory: {Path}", gameWorkingDirectory.FullName);

        string savePath = launchArguments.SavePath?.FullName ?? Path.Combine(Directory.GetCurrentDirectory(), "ModSaves");
        logger.LogInformation("SavePath: {Path}", savePath);

        instance.ModManager.LoadMods(ModLoadPhase.AfterGameAssembly);

        string oldWorkingDirectory = Directory.GetCurrentDirectory();
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

            object? result = handlerResult.EntryPoint.Invoke(null, new object[] { gameArguments.ToArray() });
            if (result is not null)
                logger.LogInformation("Cobalt Core closed with result: {Result}", result);
        }
        catch (Exception e)
        {
            logger.LogCritical("Cobalt Core threw an exception: {e}", e);
        }
        Directory.SetCurrentDirectory(oldWorkingDirectory);
    }

    [EventPriority(double.MaxValue)]
    private void OnModLoadPhaseFinished(object? sender, ModLoadPhase phase)
    {
        if (phase != ModLoadPhase.AfterDbInit)
            return;
        this.ModManager.ContentManager?.InjectQueuedEntries();
    }

    [EventPriority(double.MaxValue)]
    private void OnLoadStringsForLocale(object? sender, LoadStringsForLocaleEventArgs e)
    {
        this.ModManager.LegacyDatabase?.InjectLocalizations(e.Locale, e.Localizations);
    }

    private static DirectoryInfo GetOrCreateDefaultModLibraryDirectory()
    {
        var directoryInfo = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "ModLibrary"));
        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
        }

        return directoryInfo;
    }
}
