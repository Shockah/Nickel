using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager.Cecil;
using Nickel.Framework;

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

        RootCommand rootCommand = new("Nickel -- A modding API / modloader for the game Cobalt Core.");
        rootCommand.AddOption(debugOption);
        rootCommand.AddOption(gamePathOption);
        rootCommand.AddOption(modsPathOption);
        rootCommand.AddOption(savePathOption);

        rootCommand.SetHandler((InvocationContext context) =>
        {
            LaunchArguments launchArguments = new()
            {
                Debug = context.ParseResult.GetValueForOption(debugOption),
                GamePath = context.ParseResult.GetValueForOption(gamePathOption),
                ModsPath = context.ParseResult.GetValueForOption(modsPathOption),
                SavePath = context.ParseResult.GetValueForOption(savePathOption),
                UnmatchedParameters = context.ParseResult.UnmatchedTokens
            };
            CreateAndStartInstance(launchArguments);
        });
        return rootCommand.Invoke(args);
    }

    private static void CreateAndStartInstance(LaunchArguments launchArguments)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = false;
                options.TimestampFormat = "HH:mm:ss ";
            });
        });
        var logger = loggerFactory.CreateLogger(typeof(Nickel).Namespace!);
        Console.SetOut(new LoggerTextWriter(logger, LogLevel.Information, Console.Out));
        Console.SetError(new LoggerTextWriter(logger, LogLevel.Error, Console.Error));

        Nickel instance = new();
        Instance = instance;

        Harmony harmony = new(typeof(Nickel).Namespace!);
        HarmonyPatches.Apply(harmony, logger);

        var modsDirectory = launchArguments.ModsPath ?? new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "ModLibrary"));
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

        // game assembly loaded by now

        instance.ModManager.CobaltCoreAssembly = handlerResult.GameAssembly;

        MGPatches.Apply(harmony, logger);

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
            gameArguments.AddRange(launchArguments.UnmatchedParameters);

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
}
