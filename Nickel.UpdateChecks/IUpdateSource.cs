using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nickel.UpdateChecks;

/// <summary>
/// A type that implements update checks for mods via some service.
/// </summary>
public interface IUpdateSource
{
	/// <summary>
	/// Messages that should be presented to the user.
	/// </summary>
	IEnumerable<UpdateSourceMessage> Messages
		=> [];

	/// <summary>
	/// Attempt to deserialize a given JSON object to data specific to this update source.
	/// </summary>
	/// <param name="mod">The mod the data is being deserialized for.</param>
	/// <param name="rawManifestEntry">The JSON object.</param>
	/// <param name="manifestEntry">
	/// The resulting data specific to this update source, if succeeded.<br/>
	/// This data will be passed to <see cref="GetLatestVersionsAsync"/>.
	/// </param>
	/// <returns>Whether deserializing succeeded.</returns>
	bool TryParseManifestEntry(IModManifest mod, JObject rawManifestEntry, out object? manifestEntry);

	/// <summary>
	/// Retrieves the update data for the given mods asynchronously.
	/// </summary>
	/// <param name="mods">The mods to retrieve the update data for.</param>
	/// <returns>A task which eventually returns the requested update data.</returns>
	Task<IReadOnlyDictionary<IModManifest, UpdateDescriptor>> GetLatestVersionsAsync(IEnumerable<(IModManifest Mod, object? ManifestEntry)> mods);
}
