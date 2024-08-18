using System.IO;

namespace Nanoray.PluginManager;

/// <summary>
/// A type which edits an <see cref="System.Reflection.Assembly"/> <see cref="Stream"/> before it gets loaded.
/// </summary>
public interface IAssemblyEditor
{
	/// <summary>
	/// Edit the stream.
	/// </summary>
	/// <param name="name">The name of the assembly.</param>
	/// <param name="assemblyStream">The <see cref="System.Reflection.Assembly"/> <see cref="Stream"/></param>
	/// <param name="symbolsStream">An <see cref="System.Reflection.Assembly"/> symbols <see cref="Stream"/>, if any.</param>
	/// <returns>A result contaning any messages the editor has reported.</returns>
	AssemblyEditorResult EditAssemblyStream(string name, ref Stream assemblyStream, ref Stream? symbolsStream);
}

/// <summary>
/// Hosts extensions for <see cref="IAssemblyEditor"/>.
/// </summary>
public static class IAssemblyEditorExt
{
	/// <summary>
	/// Edit the stream.
	/// </summary>
	/// <param name="editor">The editor.</param>
	/// <param name="name">The name of the assembly.</param>
	/// <param name="assemblyStream">The <see cref="System.Reflection.Assembly"/> <see cref="Stream"/></param>
	/// <returns>A result contaning any messages the editor has reported.</returns>
	public static AssemblyEditorResult EditAssemblyStream(this IAssemblyEditor editor, string name, ref Stream assemblyStream)
	{
		Stream? symbolsStream = null;
		return editor.EditAssemblyStream(name, ref assemblyStream, ref symbolsStream);
	}
}
