using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using System.IO;
using System.Runtime.Loader;

namespace Nickel;

internal sealed class CobaltCoreHandler
{
	private readonly ILogger Logger;
	private readonly IAssemblyEditor? AssemblyEditor;

	public CobaltCoreHandler(ILogger logger, IAssemblyEditor? assemblyEditor)
	{
		this.Logger = logger;
		this.AssemblyEditor = assemblyEditor;
	}

	public OneOf<CobaltCoreHandlerResult, Error<string>> SetupGame(CobaltCoreResolveResult resolveResult)
	{
		this.Logger.LogInformation("Loading game assembly...");
		this.ResolveAssembly(
			name: "CobaltCore.dll",
			assemblyStream: resolveResult.GameAssemblyDataStream,
			symbolsStream: resolveResult.GamePdbDataStream
		);

		this.Logger.LogInformation("Loading other assemblies...");
		foreach (var (name, stream) in resolveResult.OtherDllDataStreams)
		{
			this.Logger.LogDebug("Trying to load (potential) assembly {AssemblyName}...", name);
			this.ResolveAssembly(name, stream);
		}

		return ContinueGameSetupAfterResolvingAssemblies(resolveResult);
	}

	private static OneOf<CobaltCoreHandlerResult, Error<string>> ContinueGameSetupAfterResolvingAssemblies(CobaltCoreResolveResult resolveResult)
	{
		var gameAssembly = typeof(MG).Assembly;

		if (gameAssembly.EntryPoint is not { } entryPoint)
			return new Error<string>("The Cobalt Core assembly does not contain an entry point.");
		return new CobaltCoreHandlerResult
		{
			GameAssembly = gameAssembly,
			EntryPoint = entryPoint,
			WorkingDirectory = resolveResult.WorkingDirectory
		};
	}

	private void ResolveAssembly(string name, Stream assemblyStream, Stream? symbolsStream = null)
	{
		this.AssemblyEditor?.EditAssemblyStream(name, ref assemblyStream, ref symbolsStream);
		AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
		{
			if ($"{assemblyName.Name ?? assemblyName.FullName}.dll" == name)
				return context.LoadFromStream(assemblyStream, symbolsStream);
			return null;
		};
	}
}
