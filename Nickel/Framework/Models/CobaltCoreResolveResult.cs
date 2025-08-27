using Nanoray.PluginManager;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal readonly struct CobaltCoreResolveResult
{
	public required IFileInfo ExePath { get; init; }
	public required Func<Stream> GameAssemblyDataStreamProvider { get; init; }
	public required Func<Stream>? GameSymbolsDataStreamProvider { get; init; }
	public required IReadOnlyDictionary<string, Func<Stream>> OtherDllDataStreamProviders { get; init; }
	public required IDirectoryInfo WorkingDirectory { get; init; }
}
