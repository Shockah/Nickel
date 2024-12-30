using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class CompoundAssemblyResolver(IEnumerable<IAssemblyResolver> resolvers) : IAssemblyResolver
{
	public void Dispose()
	{
		foreach (var resolver in resolvers)
			resolver.Dispose();
	}

	public AssemblyDefinition Resolve(AssemblyNameReference name)
		=> this.Resolve(name, new ReaderParameters());

	public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
	{
		parameters.AssemblyResolver ??= this;
		var exceptions = new List<Exception>();
		
		foreach (var resolver in resolvers)
		{
			try
			{
				return resolver.Resolve(name, parameters);
			}
			catch (AssemblyResolutionException)
			{
				// ignored
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}
		}

		throw exceptions.Count switch
		{
			0 => new AssemblyResolutionException(name),
			1 => exceptions[0],
			_ => new AssemblyResolutionException(name, new MultipleExceptions(exceptions)),
		};
	}

	public sealed class MultipleExceptions : Exception
	{
		public readonly IReadOnlyList<Exception> Exceptions;

		public MultipleExceptions(IReadOnlyList<Exception> exceptions) : base(string.Join("\n", exceptions.Select(ex => ex.Message)))
		{
			this.Exceptions = exceptions;
		}
	}
}
