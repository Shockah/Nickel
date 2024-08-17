using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal readonly struct CobaltCoreResolveResult
{
	public required FileInfo ExePath { get; init; }
	public required MemoryStream GameAssemblyDataStream { get; init; }
	public required MemoryStream? GamePdbDataStream { get; init; }
	public required IReadOnlyDictionary<string, MemoryStream> OtherDllDataStreams { get; init; }
	public required DirectoryInfo WorkingDirectory { get; init; }
}
