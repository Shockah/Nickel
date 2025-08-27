using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using SingleFileExtractor.Core;
using System;
using System.IO;
using System.Linq;

namespace Nickel;

internal sealed class SingleFileApplicationCobaltCoreResolver(
	IFileInfo exePath,
	IFileInfo? pdbPath,
	ILogger logger
) : ICobaltCoreResolver
{
	private const string CobaltCoreResource = "CobaltCore.dll";

	public OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore()
	{
		logger.LogTrace("Attempting to resolve Cobalt Core from path: {Path}", PathUtilities.SanitizePath(exePath.FullName));
		if (!exePath.Exists)
			return new Error<string>($"The file `{exePath.FullName}` does not exist.");

		try
		{
			var reader = new ExecutableReader(exePath.FullName);
			if (!reader.IsSingleFile)
				return new Error<string>($"The file at `{exePath.FullName}` is not a single file executable.");
			if (!reader.IsSupported)
				return new Error<string>($"The file at `{exePath.FullName}` is not supported.");

			var cobaltCoreEntry = reader.Bundle.Files.FirstOrDefault(e => e.RelativePath == CobaltCoreResource);
			if (cobaltCoreEntry is null)
				return new Error<string>($"The single-file application at `{exePath.FullName}` does not contain a `{CobaltCoreResource}` resource.");

			var otherDlls = reader.Bundle.Files.Where(e => e.RelativePath.EndsWith(".dll") && e.RelativePath != CobaltCoreResource).ToHashSet();
			var gameAssemblyData = cobaltCoreEntry.AsStream().ToMemoryStream().ToArray();
			var gameSymbolsData = pdbPath?.Exists == true ? pdbPath.OpenRead().ToMemoryStream().ToArray() : null;
			var otherDllDataStreamProviders = otherDlls.ToDictionary(
				e => e.RelativePath,
				Func<Stream> (e) =>
				{
					var data = e.AsStream().ToMemoryStream().ToArray();
					return () => new MemoryStream(data);
				}
			);
			foreach (var file in exePath.Parent!.Files.Where(f => f.Name.EndsWith(".dll") && f.Name != CobaltCoreResource))
				otherDllDataStreamProviders[file.Name] = () => file.OpenRead();
			
			logger.LogTrace("Resolved Cobalt Core path: {Path}", PathUtilities.SanitizePath(exePath.FullName));
			logger.LogTrace("Resolved Cobalt Core PDB path: {Path}", pdbPath is null ? "<null>" : PathUtilities.SanitizePath(pdbPath.FullName));

			return new CobaltCoreResolveResult
			{
				ExePath = exePath,
				GameAssemblyDataStreamProvider = () => new MemoryStream(gameAssemblyData),
				GameSymbolsDataStreamProvider = gameSymbolsData is null ? null : () => new MemoryStream(gameSymbolsData),
				OtherDllDataStreamProviders = otherDllDataStreamProviders,
				WorkingDirectory = exePath.Parent!
			};
		}
		catch (Exception ex)
		{
			return new Error<string>($"The file at `{exePath.FullName}` could not be opened as a single file executable: {ex}");
		}
	}
}
