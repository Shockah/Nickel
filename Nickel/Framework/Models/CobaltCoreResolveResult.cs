using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal readonly struct CobaltCoreResolveResult
{
	public Stream GameAssemblyDataStream { get; init; }
	public Stream? GamePdbDataStream { get; init; }
	public IReadOnlyDictionary<string, Stream> OtherDllDataStreams { get; init; }
	public DirectoryInfo WorkingDirectory { get; init; }
}
