using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Nickel;

internal class Nickel
{
    internal static int Main(string[] args)
    {
        Option<FileInfo?> modsPathOption = new(
            name: "--modsPath",
            description: "The path containing the mods to load."
        );

        RootCommand rootCommand = new("Nickel -- A modding API / modloader for the game Cobalt Core.");
        rootCommand.AddOption(modsPathOption);

        rootCommand.SetHandler((InvocationContext context) =>
        {
            var modsPath = context.ParseResult.GetValueForOption(modsPathOption);
        });
        return rootCommand.Invoke(args);
    }
}
