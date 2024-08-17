using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nanoray.PluginManager.Cecil;

/// <summary>
/// An <see cref="IAssemblyEditor"/> which allows registering <see cref="IAssemblyDefinitionEditor"/> implementations.
/// </summary>
/// <param name="cecilAssemblyResolverProducer">An <see cref="IAssemblyResolver"/> provider function.</param>
public sealed class ExtendableAssemblyDefinitionEditor(Func<IAssemblyResolver> cecilAssemblyResolverProducer) : IAssemblyEditor
{
	private readonly List<IAssemblyDefinitionEditor> DefinitionEditors = [];

	/// <inheritdoc/>
	public void EditAssemblyStream(string name, ref Stream assemblyStream, ref Stream? symbolsStream)
	{
		var interestedEditors = this.DefinitionEditors.Where(x => x.WillEditAssembly(name)).ToList();
		if (interestedEditors.Count <= 0)
			return;

		using var assemblyResolver = cecilAssemblyResolverProducer();
		AssemblyDefinition readDefinition;
		
		var assemblyMemoryStream = new MemoryStream();
		assemblyStream.CopyTo(assemblyMemoryStream);
		assemblyMemoryStream.Position = 0;
		assemblyStream.Dispose();
		assemblyStream = assemblyMemoryStream;

		MemoryStream? symbolsMemoryStream = null;
		if (symbolsStream is not null)
		{
			symbolsMemoryStream = new();
			symbolsStream.CopyTo(symbolsMemoryStream);
			symbolsMemoryStream.Position = 0;
			symbolsStream.Dispose();
			symbolsStream = symbolsMemoryStream;
		}

		try
		{
			readDefinition = AssemblyDefinition.ReadAssembly(assemblyMemoryStream, new ReaderParameters
			{
				AssemblyResolver = assemblyResolver,
				ReadSymbols = symbolsMemoryStream is not null,
				SymbolStream = symbolsMemoryStream
			});
		}
		catch (BadImageFormatException)
		{
			// most likely not a managed DLL
			assemblyMemoryStream.Position = 0;
			if (symbolsMemoryStream is not null)
				symbolsMemoryStream.Position = 0;
			return;
		}

		using var definition = readDefinition;
		
		var didAnything = false;
		foreach (var definitionEditor in interestedEditors)
			didAnything |= definitionEditor.EditAssemblyDefinition(definition);

		if (!didAnything)
		{
			assemblyMemoryStream.Position = 0;
			if (symbolsMemoryStream is not null)
				symbolsMemoryStream.Position = 0;
			return;
		}

		var newAssemblyStream = new MemoryStream();
		var newSymbolsStream = symbolsStream is null ? null : new MemoryStream();

		definition.Write(newAssemblyStream, new WriterParameters
		{
			WriteSymbols = symbolsStream is not null,
			SymbolStream = newSymbolsStream,
			SymbolWriterProvider = symbolsStream is null ? null : new PortablePdbWriterProvider()
		});

		newAssemblyStream.Position = 0;
		if (newSymbolsStream is not null)
			newSymbolsStream.Position = 0;

		assemblyStream = newAssemblyStream;
		symbolsStream = newSymbolsStream;
	}
	
	/// <summary>
	/// Register a definition editor.
	/// </summary>
	/// <param name="definitionEditor">The definition editor.</param>
	public void RegisterDefinitionEditor(IAssemblyDefinitionEditor definitionEditor)
		=> this.DefinitionEditors.Add(definitionEditor);

	/// <summary>
	/// Unregister a definition editor.
	/// </summary>
	/// <param name="definitionEditor">The definition editor.</param>
	public void UnregisterDefinitionEditor(IAssemblyDefinitionEditor definitionEditor)
		=> this.DefinitionEditors.Remove(definitionEditor);
}
