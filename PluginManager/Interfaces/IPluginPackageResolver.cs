using System.Collections.Generic;
using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager;

public interface IPluginPackageResolver<TPluginManifest>
{
	IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages();
}
