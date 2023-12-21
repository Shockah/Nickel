using System.Collections.Generic;
using OneOf;
using OneOf.Types;

namespace Shockah.PluginManager;

public interface IPluginPackageResolver<TPluginManifest>
{
    IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages();
}
