using OneOf;
using OneOf.Types;
using SingleFileExtractor.Core;
using System;
using System.IO;
using System.Linq;

namespace Nickel;

internal sealed class SingleFileApplicationCobaltCoreResolver(FileInfo exePath, FileInfo? pdbPath) : ICobaltCoreResolver
{
	private const string CobaltCoreResource = "CobaltCore.dll";

	public OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore()
	{
		if (!exePath.Exists)
			return new Error<string>($"The file `{exePath.FullName}` does not exist.");

		var reader = new ExecutableReader(exePath.FullName);
		if (!reader.IsSingleFile)
			return new Error<string>($"The file at `{exePath.FullName}` is not a single file executable.");
		if (!reader.IsSupported)
			return new Error<string>($"The file at `{exePath.FullName}` is not supported.");

		var cobaltCoreEntry = reader.Bundle.Files.FirstOrDefault(e => e.RelativePath == CobaltCoreResource);
		if (cobaltCoreEntry is null)
			return new Error<string>($"The single-file application at `{exePath.FullName}` does not contain a `{CobaltCoreResource}` resource.");

		var otherDlls = reader.Bundle.Files.Where(e => e.RelativePath.EndsWith(".dll") && e.RelativePath != CobaltCoreResource).ToHashSet();
		var gameAssemblyData = cobaltCoreEntry.AsStream().ToMemoryStream().GetBuffer();
		var gameSymbolsData = pdbPath?.Exists == true ? pdbPath.OpenRead().ToMemoryStream().GetBuffer() : null;
		var otherDllDataStreamProviders = otherDlls.ToDictionary(
			e => e.RelativePath,
			Func<Stream> (e) =>
			{
				var data = e.AsStream().ToMemoryStream().GetBuffer();
				return () => new MemoryStream(data);
			}
		);
		foreach (var file in exePath.Directory!.GetFiles("*.dll", SearchOption.TopDirectoryOnly).Where(f => f.Name != CobaltCoreResource))
			otherDllDataStreamProviders[file.Name] = () => file.OpenRead();

		return new CobaltCoreResolveResult
		{
			ExePath = exePath,
			GameAssemblyDataStreamProvider = () => new MemoryStream(gameAssemblyData),
			GameSymbolsDataStreamProvider = gameSymbolsData is null ? null : () => new MemoryStream(gameSymbolsData),
			OtherDllDataStreamProviders = otherDllDataStreamProviders,
			WorkingDirectory = exePath.Directory!
		};
	}
}
