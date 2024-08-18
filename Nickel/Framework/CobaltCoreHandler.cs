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
			assemblyStream: resolveResult.GameAssemblyDataStreamProvider(),
			symbolsStream: resolveResult.GameSymbolsDataStreamProvider?.Invoke()
		);

		this.Logger.LogInformation("Loading other assemblies...");
		foreach (var (name, streamProvider) in resolveResult.OtherDllDataStreamProviders)
		{
			this.Logger.LogDebug("Trying to load (potential) assembly {AssemblyName}...", name);
			this.ResolveAssembly(name, streamProvider());
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
		if (this.AssemblyEditor?.EditAssemblyStream(name, ref assemblyStream, ref symbolsStream) is { } editorResult)
			foreach (var message in editorResult.Messages)
				this.Logger.Log(message.Level switch
				{
					AssemblyEditorResult.MessageLevel.Error => LogLevel.Error,
					AssemblyEditorResult.MessageLevel.Warning => LogLevel.Warning,
					AssemblyEditorResult.MessageLevel.Info => LogLevel.Information,
					AssemblyEditorResult.MessageLevel.Debug => LogLevel.Debug,
					_ => LogLevel.Error
				}, message.Content);
		
		AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
		{
			try
			{
				if ($"{assemblyName.Name ?? assemblyName.FullName}.dll" == name)
					return context.LoadFromStream(assemblyStream, symbolsStream);
			}
			catch
			{
				// ignored
			}
			return null;
		};
	}
}
