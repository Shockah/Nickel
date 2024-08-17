using Mono.Cecil;
using Nanoray.PluginManager;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal class PackageAssemblyStreamResolver(IReadOnlyList<IPluginPackage<IModManifest>> packages) : IAssemblyStreamResolver
{
	private readonly Dictionary<string, AssemblyDefinition> Cache = [];
	private readonly List<Stream> OpenStreams = [];

	public void Dispose() { }

	public Stream? Resolve(AssemblyNameReference name)
	{
		foreach (var package in packages)
		{
			try
			{
				return package.PackageRoot.GetRelativeFile(name.Name + ".dll").OpenRead();
			}
			catch (FileNotFoundException) { }

			try
			{
				return package.PackageRoot.GetRelativeFile(name.Name + ".exe").OpenRead();
			}
			catch (FileNotFoundException) { }
		}

		return null;
	}

}
