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
	void EditAssemblyStream(string name, ref Stream assemblyStream, ref Stream? symbolsStream);
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
	public static void EditAssemblyStream(this IAssemblyEditor editor, string name, ref Stream assemblyStream)
	{
		Stream? symbolsStream = null;
		editor.EditAssemblyStream(name, ref assemblyStream, ref symbolsStream);
	}
}
