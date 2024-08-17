using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal interface IAssemblyStreamResolver : IDisposable
{
	public Stream? Resolve(AssemblyNameReference name);
}

internal class NickelAssemblyResolver(IAssemblyStreamResolver streamResolver, IAssemblyResolver fallbackResolver) : IAssemblyResolver
{
	private readonly Dictionary<string, AssemblyDefinition> Cache = [];
	private readonly List<Stream> OpenStreams = [];

	public void Dispose()
	{
		foreach (var assembly in this.Cache.Values)
			assembly.Dispose();
		foreach (var stream in this.OpenStreams)
			stream.Dispose();
		fallbackResolver.Dispose();
	}

	public AssemblyDefinition Resolve(AssemblyNameReference name)
	{
		if (this.Cache.TryGetValue(name.FullName, out var cached))
			return cached;
		return this.Cache[name.FullName] = this.Resolve(name, new ReaderParameters());
	}

	public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
	{
		parameters.AssemblyResolver ??= this;
		var stream = streamResolver.Resolve(name);

		if(stream is null)
			return fallbackResolver.Resolve(name, parameters);

		try
		{
			this.OpenStreams.Add(stream);
		} catch (Exception)
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
}
