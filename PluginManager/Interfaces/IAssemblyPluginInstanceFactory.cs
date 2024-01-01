using OneOf;
using OneOf.Types;
using System.Collections.Generic;
using System.Reflection;

namespace Nanoray.PluginManager;

public interface IAssemblyPluginPartAssembler<in TPluginManifest, TPlugin, TPluginPart>
{
	OneOf<TPlugin, Error<string>> AssemblePluginFromParts(IPluginPackage<TPluginManifest> package, Assembly assembly, IReadOnlySet<TPluginPart> parts);
}
