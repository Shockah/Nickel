using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal readonly struct CobaltCoreResolveResult
{
	public required FileInfo ExePath { get; init; }
	public required Stream GameAssemblyDataStream { get; init; }
	public required Stream? GamePdbDataStream { get; init; }
	public required IReadOnlyDictionary<string, Stream> OtherDllDataStreams { get; init; }
	public required DirectoryInfo WorkingDirectory { get; init; }
}
