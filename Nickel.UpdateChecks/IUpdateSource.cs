using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nickel.UpdateChecks;

public interface IUpdateSource
{
	IEnumerable<UpdateSourceMessage> Messages
		=> [];

	bool TryParseManifestEntry(IModManifest mod, object? rawManifestEntry, out object? manifestEntry);

	Task<IReadOnlyDictionary<IModManifest, UpdateDescriptor>> GetLatestVersionsAsync(IEnumerable<(IModManifest Mod, object? ManifestEntry)> mods);
}
