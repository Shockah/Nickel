using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Nickel;

internal class Nickel
{
    internal static Nickel Instance { get; private set; } = null!;

    private ILogger Logger { get; init; }
    private Assembly GameAssembly { get; init; }
    private ModManager ModManager { get; init; }

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

        RootCommand rootCommand = new("Nickel -- A modding API / modloader for the game Cobalt Core.");
        rootCommand.AddOption(modsPathOption);

        rootCommand.SetHandler((InvocationContext context) =>
        {
            LaunchArguments launchArguments = new()
            {
                GamePath = context.ParseResult.GetValueForOption(gamePathOption),
                ModsPath = context.ParseResult.GetValueForOption(modsPathOption)
            };
            CreateAndStartInstance(launchArguments);
        });
        return rootCommand.Invoke(args);
    }

    private static void CreateAndStartInstance(LaunchArguments launchArguments)
    {
        var logger = LoggerFactory.Create(b => { }).CreateLogger(typeof(Nickel).Namespace!);

        CobaltCoreHandler handler = new(
            launchArguments.GamePath is { } gamePath
                ? new SingleFileApplicationCobaltCoreResolver(gamePath, null) // TODO: pass in the PDB path too
                : new SteamCobaltCoreResolver(f => new SingleFileApplicationCobaltCoreResolver(f, null)) // TODO: pass in the PDB path too
        );

        var handlerResultOrError = handler.SetupGame();
        if (handlerResultOrError.TryPickT1(out var error, out var handlerResult))
        {
            logger.LogCritical("Could not start the game: {Error}", error.Value);
            return;
        }

        ModManager modManager = new(
            modsPath: launchArguments.ModsPath ?? new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "ModLibrary")),
            logger: logger
        );

        Nickel instance = new(logger, handlerResult.GameAssembly, modManager);
        Instance = instance;
    }

    public Nickel(ILogger logger, Assembly gameAssembly, ModManager modManager)
    {
        this.Logger = logger;
        this.GameAssembly = gameAssembly;
        this.ModManager = modManager;
    }
}
