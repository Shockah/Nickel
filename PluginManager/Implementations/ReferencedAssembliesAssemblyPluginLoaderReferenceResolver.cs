using System.Collections.Generic;
using System.Reflection;

namespace Nanoray.PluginManager;

public sealed class ReferencedAssembliesAssemblyPluginLoaderReferenceResolver<TPluginManifest> : IAssemblyPluginLoaderReferenceResolver<TPluginManifest>
{
	public IEnumerable<IFileInfo> ResolveAssemblyReferences(IPluginPackage<TPluginManifest> package, Assembly assembly)
	{
		foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
			yield return package.PackageRoot.GetRelativeFile($"{referencedAssembly.Name ?? referencedAssembly.FullName}.dll");
	}
}
