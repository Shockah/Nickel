using OneOf;
using OneOf.Types;
using System.IO;

namespace Nanoray.PluginManager;

public interface IPluginManifestLoader<TPluginManifest>
{
	OneOf<TPluginManifest, Error<string>> LoadPluginManifest(Stream stream);
}
