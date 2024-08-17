using Mono.Cecil;
using System.IO;

namespace Nickel;

internal class PreloadAssemblyStreamResolver(CobaltCoreResolveResult resolveResult, IAssemblyStreamResolver fallbackResolver) : IAssemblyStreamResolver
{
	public void Dispose() { }

	public Stream? Resolve(AssemblyNameReference name)
	{
		if(name.Name == "CobaltCore")
		{
			return new BorrowedMemoryStream(resolveResult.GameAssemblyDataStream);
		}

		if(resolveResult.OtherDllDataStreams.TryGetValue(name.Name + ".dll", out var stream))
			return new BorrowedMemoryStream(stream);

		return fallbackResolver.Resolve(name);
	}
}
