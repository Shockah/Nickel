using System.IO;
using System.Linq;
using OneOf;
using OneOf.Types;
using SingleFileExtractor.Core;

namespace Nickel;

internal sealed class SingleFileApplicationCobaltCoreResolver : ICobaltCoreResolver
{
    private const string CobaltCoreResource = "CobaltCore.dll";

    private FileInfo ExePath { get; init; }
    private FileInfo? PdbPath { get; init; }

    public SingleFileApplicationCobaltCoreResolver(FileInfo exePath, FileInfo? pdbPath)
    {
        this.ExePath = exePath;
        this.PdbPath = pdbPath;
    }

    public OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore()
    {
        if (!this.ExePath.Exists)
            return new Error<string>($"The file `{this.ExePath.FullName}` does not exist.");

        ExecutableReader reader = new(this.ExePath.FullName);
        if (!reader.IsSingleFile)
            return new Error<string>($"The file at `{this.ExePath.FullName}` is not a single file executable.");
        if (!reader.IsSupported)
            return new Error<string>($"The file at `{this.ExePath.FullName}` is not supported.");

        var cobaltCoreEntry = reader.Bundle.Files.FirstOrDefault(e => e.RelativePath == CobaltCoreResource);
        if (cobaltCoreEntry is null)
            return new Error<string>($"The single-file application at `{this.ExePath.FullName}` does not contain a `{CobaltCoreResource}` resource.");

        var otherDlls = reader.Bundle.Files.Where(e => e.RelativePath.EndsWith(".dll") && e.RelativePath != CobaltCoreResource).ToHashSet();
        var gameAssemblyDataStream = cobaltCoreEntry.AsStream().ToMemoryStream();
        var gamePdbDataStream = this.PdbPath?.Exists == true ? this.PdbPath.OpenRead().ToMemoryStream() : null;
        var otherDllDataStreams = otherDlls.ToDictionary(e => e.RelativePath, e => (Stream)e.AsStream().ToMemoryStream());
        return new CobaltCoreResolveResult { GameAssemblyDataStream = gameAssemblyDataStream, GamePdbDataStream = gamePdbDataStream, OtherDllDataStreams = otherDllDataStreams };
    }
}
