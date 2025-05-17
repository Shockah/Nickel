using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using System;

namespace Nickel;

internal sealed class RecursiveToRootDirectoryCobaltCoreResolver(
	IDirectoryInfo baseDirectory,
	Func<IDirectoryInfo, ICobaltCoreResolver?> resolverFactory
) : ICobaltCoreResolver
{
	public OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore()
	{
		var currentDirectory = baseDirectory;
		while (currentDirectory?.Exists ?? false)
		{
			if (resolverFactory(currentDirectory) is { } resolver)
				return resolver.ResolveCobaltCore();
			currentDirectory = currentDirectory.Parent;
		}
		return new Error<string>($"Could not resolve Cobalt Core in any parent directories of `{baseDirectory}`.");
	}
}
