using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using System;

namespace Nickel;

internal sealed class RecursiveToRootDirectoryCobaltCoreResolver(
	IDirectoryInfo baseDirectory,
	Func<IDirectoryInfo, ICobaltCoreResolver?> resolverFactory,
	ILogger logger
) : ICobaltCoreResolver
{
	public OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore()
	{
		logger.LogTrace("Attempting to resolve Cobalt Core recursively up to root directory...");
		var currentDirectory = baseDirectory;
		while (currentDirectory?.Exists ?? false)
		{
			logger.LogTrace("Attempting to resolve from path: {Path}", PathUtilities.SanitizePath(currentDirectory.FullName));
			if (resolverFactory(currentDirectory) is { } resolver)
				return resolver.ResolveCobaltCore();
			currentDirectory = currentDirectory.Parent;
		}
		return new Error<string>($"Could not resolve Cobalt Core in any parent directories of `{baseDirectory}`.");
	}
}
