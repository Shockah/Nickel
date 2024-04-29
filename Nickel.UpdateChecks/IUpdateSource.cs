using Nickel.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nickel.UpdateChecks;

public interface IUpdateSource
{
	bool TryParseManifestEntry(IModManifest mod, object? rawManifestEntry, out object? manifestEntry);

	Task<IReadOnlyDictionary<IModManifest, (SemanticVersion Version, string UpdateInfo)>> GetLatestVersionsAsync(IEnumerable<(IModManifest Mod, object? ManifestEntry)> mods);
}
