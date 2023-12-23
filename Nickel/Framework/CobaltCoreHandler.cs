using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace Nickel;

internal sealed class CobaltCoreHandler
{
    private ILogger Logger { get; init; }
    private ICobaltCoreResolver Resolver { get; init; }

    public CobaltCoreHandler(ILogger logger, ICobaltCoreResolver resolver)
    {
        this.Logger = logger;
        this.Resolver = resolver;
    }

    public OneOf<CobaltCoreHandlerResult, Error<string>> SetupGame()
    {
        var resolveResultOrError = this.Resolver.ResolveCobaltCore();
        if (resolveResultOrError.TryPickT1(out var error, out var resolveResult))
            return new Error<string>($"Could not resolve Cobalt Core: {error.Value}");

        this.Logger.LogInformation("Loading game assembly...");
        var gameAssembly = LoadAssembly(
            assemblyStream: resolveResult.GameAssemblyDataStream,
            symbolsStream: resolveResult.GamePdbDataStream
        );

        AppDomain.CurrentDomain.AssemblyResolve += (_, e) => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == e.Name);

        this.Logger.LogInformation("Loading other assemblies...");
        foreach (var (name, stream) in resolveResult.OtherDllDataStreams)
        {
            this.Logger.LogDebug("Trying to load (potential) assembly {AssemblyName}...", name);
            try
            {
                LoadAssembly(stream);
            }
            catch (BadImageFormatException e)
            {
                this.Logger.LogDebug("Failed to load {AssemblyName}: {Exception}", name, e);
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

    private Assembly LoadAssembly(Stream assemblyStream, Stream? symbolsStream = null)
    {
        AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(GetType().Assembly)
            ?? AssemblyLoadContext.CurrentContextualReflectionContext
            ?? AssemblyLoadContext.Default;
        return context.LoadFromStream(assemblyStream, symbolsStream);
    }
}
