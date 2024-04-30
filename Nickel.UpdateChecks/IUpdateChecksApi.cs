using System;

namespace Nickel.UpdateChecks;

public interface IUpdateChecksApi
{
	bool TryGetUpdateInfo(IModManifest mod, out UpdateDescriptor? update);
	void AwaitUpdateInfo(IModManifest mod, Action<IModManifest, UpdateDescriptor?> callback);

	void RegisterUpdateSource(string sourceKey, IUpdateSource source);
}
