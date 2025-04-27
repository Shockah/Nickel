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
	public byte[] AssemblyEditorDescriptor
	{
		get
		{
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream);
			
			writer.Write(this.DefinitionEditors.Count);
			foreach (var editor in this.DefinitionEditors)
			{
				var editorDescriptor = editor.AssemblyEditorDescriptor;
				writer.Write(editorDescriptor.Length);
				writer.Write(editorDescriptor);
			}
			
			writer.Flush();
			return stream.ToArray();
		}
	}

	/// <inheritdoc/>
	public AssemblyEditorResult EditAssemblyStream(string name, ref Stream assemblyStream, ref Stream? symbolsStream)
	{
		var messages = new List<AssemblyEditorResult.Message>();
		var interestedEditors = this.DefinitionEditors.Where(x => x.WillEditAssembly(name)).ToList();
		if (interestedEditors.Count <= 0)
			return new() { Messages = messages };

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
			symbolsMemoryStream = new MemoryStream();
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
			return new() { Messages = messages };
		}

		using var definition = readDefinition;
		var didAnything = false;

		if (definition.Modules.All(m => (m.Attributes & ModuleAttributes.ILOnly) != 0))
			foreach (var definitionEditor in interestedEditors)
				didAnything |= definitionEditor.EditAssemblyDefinition(definition, message => messages.Add(message));

		if (!didAnything)
		{
			assemblyMemoryStream.Position = 0;
			if (symbolsMemoryStream is not null)
				symbolsMemoryStream.Position = 0;
			return new() { Messages = messages };
		}

		var newAssemblyStream = new MemoryStream();
		var newSymbolsStream = symbolsStream is null ? null : new MemoryStream();

		definition.Write(newAssemblyStream, new WriterParameters
		{
			WriteSymbols = symbolsStream is not null,
			SymbolStream = newSymbolsStream,
			SymbolWriterProvider = newSymbolsStream is null ? null : new PortablePdbWriterProvider()
		});

		newAssemblyStream.Position = 0;
		if (newSymbolsStream is not null)
			newSymbolsStream.Position = 0;

		assemblyStream = newAssemblyStream;
		symbolsStream = newSymbolsStream;
		return new() { Messages = messages };
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
