using Mono.Cecil;
using System;

namespace Nanoray.PluginManager.Cecil;

/// <summary>
/// A type which edits an <see cref="AssemblyDefinition"/> before it gets loaded.
/// </summary>
public interface IAssemblyDefinitionEditor
{
	/// <summary>
	/// Allows controlling whether this editor is interested in editing the given assembly.
	/// </summary>
	/// <param name="fileBaseName">The base file name of the assembly.</param>
	/// <returns>Whether this editor is interested in editing the given assembly.</returns>
	/// <remarks>If no editors are interested in a given assembly, the assembly will not be preloaded as an <see cref="AssemblyDefinition"/>, decreasing load times.</remarks>
	bool WillEditAssembly(string fileBaseName);

	/// <summary>
	/// Edit the assembly definition.
	/// </summary>
	/// <param name="definition">The assembly definition.</param>
	/// <param name="logger">A function that logs any messages the editor has to report.</param>
	/// <returns>Whether the assembly was actually edited.</returns>
	bool EditAssemblyDefinition(AssemblyDefinition definition, Action<AssemblyEditorResult.Message> logger);
}
