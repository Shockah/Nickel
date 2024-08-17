using OneOf;
using OneOf.Types;
using SingleFileExtractor.Core;
using System;
using System.IO;
using System.Linq;

namespace Nickel;

internal sealed class SingleFileApplicationCobaltCoreResolver : ICobaltCoreResolver
{
	private const string CobaltCoreResource = "CobaltCore.dll";

	private readonly FileInfo ExePath;
	private readonly FileInfo? PdbPath;

	public SingleFileApplicationCobaltCoreResolver(FileInfo exePath, FileInfo? pdbPath)
	{
		this.ExePath = exePath;
		this.PdbPath = pdbPath;
	}

	public OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore()
	{
		if (!this.ExePath.Exists)
			return new Error<string>($"The file `{this.ExePath.FullName}` does not exist.");

		var reader = new ExecutableReader(this.ExePath.FullName);
		if (!reader.IsSingleFile)
			return new Error<string>($"The file at `{this.ExePath.FullName}` is not a single file executable.");
		if (!reader.IsSupported)
			return new Error<string>($"The file at `{this.ExePath.FullName}` is not supported.");

		var cobaltCoreEntry = reader.Bundle.Files.FirstOrDefault(e => e.RelativePath == CobaltCoreResource);
		if (cobaltCoreEntry is null)
			return new Error<string>($"The single-file application at `{this.ExePath.FullName}` does not contain a `{CobaltCoreResource}` resource.");

		var otherDlls = reader.Bundle.Files.Where(e => e.RelativePath.EndsWith(".dll") && e.RelativePath != CobaltCoreResource).ToHashSet();
		var gameAssemblyData = cobaltCoreEntry.AsStream().ToMemoryStream().GetBuffer();
		var gameSymbolsData = this.PdbPath?.Exists == true ? this.PdbPath.OpenRead().ToMemoryStream().GetBuffer() : null;
		var otherDllDataStreamProviders = otherDlls.ToDictionary(
			e => e.RelativePath,
			Func<Stream> (e) =>
			{
				var data = e.AsStream().ToMemoryStream().GetBuffer();
				return () => new MemoryStream(data);
			}
		);
		foreach (var file in this.ExePath.Directory!.GetFiles("*.dll", SearchOption.TopDirectoryOnly).Where(f => f.Name != CobaltCoreResource))
			otherDllDataStreamProviders[file.Name] = () => file.OpenRead();

		return new CobaltCoreResolveResult
		{
			ExePath = this.ExePath,
			GameAssemblyDataStreamProvider = () => new MemoryStream(gameAssemblyData),
			GameSymbolsDataStreamProvider = gameSymbolsData is null ? null : () => new MemoryStream(gameSymbolsData),
			OtherDllDataStreamProviders = otherDllDataStreamProviders,
			WorkingDirectory = this.ExePath.Directory!
		};
	}
}
