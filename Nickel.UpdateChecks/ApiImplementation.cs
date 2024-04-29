using Nickel.Common;
using System;

namespace Nickel.UpdateChecks;

public sealed class ApiImplementation : IUpdateChecksApi
{
	public bool TryGetUpdateInfo(IModManifest mod, out (SemanticVersion Version, string UpdateInfo)? update)
		=> ModEntry.Instance.UpdatesAvailable.TryGetValue(mod, out update);

	public void AwaitUpdateInfo(IModManifest mod, Action<IModManifest, (SemanticVersion Version, string UpdateInfo)?> callback)
	{
		if (this.TryGetUpdateInfo(mod, out var update))
		{
			callback(mod, update);
			return;
		}
		ModEntry.Instance.AwaitingUpdateInfo.Add(() => this.AwaitUpdateInfo(mod, callback));
	}

	public void RegisterUpdateSource(string sourceKey, IUpdateSource source)
		=> ModEntry.Instance.UpdateSources.Add(sourceKey, source);
}
