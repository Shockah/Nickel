using System.IO;
using OneOf;
using OneOf.Types;

namespace Shockah.PluginManager;

public interface IPluginManifestLoader<TPluginManifest>
{
    OneOf<TPluginManifest, Error<string>> LoadPluginManifest(Stream stream);
}
