using Nickel.Common;
using System;

namespace Nickel.UpdateChecks;

public interface IUpdateChecksApi
{
	bool TryGetUpdateInfo(IModManifest mod, out (SemanticVersion Version, string UpdateInfo)? update);
	void AwaitUpdateInfo(IModManifest mod, Action<IModManifest, (SemanticVersion Version, string UpdateInfo)?> callback);

	void RegisterUpdateSource(string sourceKey, IUpdateSource source);
}
