using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Microsoft.Extensions.Logging;
using Nickel.Framework;

namespace Nickel;

internal sealed class Nickel
{
    internal static int Main(string[] args)
    {
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
        rootCommand.AddOption(modsPathOption);

        rootCommand.SetHandler((InvocationContext context) =>
        {
            LaunchArguments launchArguments = new()
            {
                GamePath = context.ParseResult.GetValueForOption(gamePathOption),
                ModsPath = context.ParseResult.GetValueForOption(modsPathOption),
                SavePath = context.ParseResult.GetValueForOption(savePathOption),
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

        CobaltCoreHandler handler = new(
            logger,
            launchArguments.GamePath is { } gamePath
                ? new SingleFileApplicationCobaltCoreResolver(gamePath, new FileInfo(Path.Combine(gamePath.Directory!.FullName, "CobaltCore.pdb")))
                : new SteamCobaltCoreResolver((exe, pdb) => new SingleFileApplicationCobaltCoreResolver(exe, pdb))
        );

        var handlerResultOrError = handler.SetupGame();
        if (handlerResultOrError.TryPickT1(out var error, out var handlerResult))
        {
            logger.LogCritical("Could not start the game: {Error}", error.Value);
            return;
        }

        var gameWorkingDirectory = launchArguments.GamePath?.Directory ?? handlerResult.WorkingDirectory;
        logger.LogInformation("GameWorkingDirectory: {Path}", gameWorkingDirectory.FullName);

        var modsDirectory = launchArguments.ModsPath ?? new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "ModLibrary"));
        logger.LogInformation("ModsPath: {Path}", modsDirectory.FullName);

        string savePath = launchArguments.SavePath?.FullName ?? Path.Combine(Directory.GetCurrentDirectory(), "ModSaves");
        logger.LogInformation("SavePath: {Path}", savePath);

        // game assembly loaded by now

        ModManager modManager = new(modsDirectory: modsDirectory, logger: logger);

        string oldWorkingDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(gameWorkingDirectory.FullName);

        logger.LogInformation("Starting the game...");
        FeatureFlags.Modded = true;
        FeatureFlags.OverrideSaveLocation = savePath;
        try
        {
            object? result = handlerResult.EntryPoint.Invoke(null, new object[] { new string[] { "--debug" } });
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
