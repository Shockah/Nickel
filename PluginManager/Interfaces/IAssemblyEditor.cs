using System.IO;

namespace Nanoray.PluginManager;

public interface IAssemblyEditor
{
	void EditAssemblyStream(string name, ref Stream assemblyStream, ref Stream? symbolsStream);
}

public static class IAssemblyEditorExt
{
	public static void EditAssemblyStream(this IAssemblyEditor editor, string name, ref Stream assemblyStream)
	{
		Stream? symbolsStream = null;
		editor.EditAssemblyStream(name, ref assemblyStream, ref symbolsStream);
	}
}
