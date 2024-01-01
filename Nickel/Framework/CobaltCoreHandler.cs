using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Nickel;

internal sealed class CobaltCoreHandler
{
	private ILogger Logger { get; }
	private ICobaltCoreResolver Resolver { get; }
	private IAssemblyEditor? AssemblyEditor { get; }

	public CobaltCoreHandler(ILogger logger, ICobaltCoreResolver resolver, IAssemblyEditor? assemblyEditor)
	{
		this.Logger = logger;
		this.Resolver = resolver;
		this.AssemblyEditor = assemblyEditor;
	}

	public OneOf<CobaltCoreHandlerResult, Error<string>> SetupGame()
	{
		var resolveResultOrError = this.Resolver.ResolveCobaltCore();
		if (resolveResultOrError.TryPickT1(out var error, out var resolveResult))
			return new Error<string>($"Could not resolve Cobalt Core: {error.Value}");

		this.Logger.LogInformation("Loading game assembly...");
		var gameAssembly = this.LoadAssembly(
			name: "CobaltCore.dll",
			assemblyStream: resolveResult.GameAssemblyDataStream,
			symbolsStream: resolveResult.GamePdbDataStream
		);

		this.Logger.LogInformation("Loading other assemblies...");
		foreach (var (name, stream) in resolveResult.OtherDllDataStreams)
		{
			if (name == "System.Private.CoreLib.dll") // loading it always throws
				continue;

			this.Logger.LogDebug("Trying to load (potential) assembly {AssemblyName}...", name);
			try
			{
				this.LoadAssembly(name, stream);
			}
			catch (BadImageFormatException e)
			{
				this.Logger.LogTrace("Failed to load {AssemblyName}: {Exception}", name, e);
			}
			catch (FileLoadException e)
			{
				this.Logger.LogWarning("Failed to load {AssemblyName}: {Exception}", name, e);
			}
		}

		if (gameAssembly.EntryPoint is not { } entryPoint)
			return new Error<string>($"The Cobalt Core assembly does not contain an entry point.");
		return new CobaltCoreHandlerResult
		{
			GameAssembly = gameAssembly,
			EntryPoint = entryPoint,
			WorkingDirectory = resolveResult.WorkingDirectory
		};
	}

	private Assembly LoadAssembly(string name, Stream assemblyStream, Stream? symbolsStream = null)
	{
		var context = AssemblyLoadContext.GetLoadContext(this.GetType().Assembly)
			?? AssemblyLoadContext.CurrentContextualReflectionContext
			?? AssemblyLoadContext.Default;
		if (this.AssemblyEditor is { } assemblyEditor)
			assemblyStream = assemblyEditor.EditAssemblyStream(name, assemblyStream);
		return context.LoadFromStream(assemblyStream, symbolsStream);
	}
}
