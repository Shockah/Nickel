using Mono.Cecil;
using Nanoray.PluginManager;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal class PackageAssemblyResolver(IReadOnlyList<IPluginPackage<IModManifest>> packages) : IAssemblyResolver
{
	private readonly Dictionary<string, AssemblyDefinition> cache = [];
	private readonly List<Stream> openStreams = [];
	private readonly DefaultAssemblyResolver fallbackResolver = new();

	public void Dispose()
	{
		foreach (var assembly in this.cache.Values)
			assembly.Dispose();
		foreach (var stream in this.openStreams)
			stream.Dispose();
		this.fallbackResolver.Dispose();
	}

	public AssemblyDefinition Resolve(AssemblyNameReference name)
	{
		if (this.cache.TryGetValue(name.FullName, out var cached))
			return cached;
		return this.cache[name.FullName] = this.Resolve(name, new ReaderParameters());
	}

	public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
	{
		parameters.AssemblyResolver ??= this;
		var stream = this.GetStreamForAssembly(name);
		if (stream == null)
			return this.fallbackResolver.Resolve(name, parameters);

		try
		{
			this.openStreams.Add(stream);
		}
		catch (Exception)
		{
			stream.Dispose();
			throw;
		}

		/* `DefaultAssemblyResolver` does this as well.
		 * `AssemblyDefinition.ReadAssembly` does a similar thing internally, too.
		 * Don't ask me why, ask the Cecil authors.
		 */
		return ModuleDefinition.ReadModule(stream, parameters).Assembly;
	}

	private Stream? GetStreamForAssembly(AssemblyNameReference name)
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
