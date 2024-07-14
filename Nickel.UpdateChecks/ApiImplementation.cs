using System;

namespace Nickel.UpdateChecks;

public sealed class ApiImplementation : IUpdateChecksApi
{
	public string GetModNameForUpdatePurposes(IModManifest mod)
		=> ModEntry.GetModNameForUpdatePurposes(mod);
	
	public bool TryGetUpdateInfo(IModManifest mod, out UpdateDescriptor? update)
		=> ModEntry.Instance.UpdatesAvailable.TryGetValue(mod, out update);

	public void AwaitUpdateInfo(IModManifest mod, Action<IModManifest, UpdateDescriptor?> callback)
	{
		if (this.TryGetUpdateInfo(mod, out var update))
		{
			callback(mod, update);
			return;
		}
		ModEntry.Instance.AwaitingUpdateInfo.Add(() => this.AwaitUpdateInfo(mod, callback));
	}

	public void RequestUpdateInfo(IUpdateSource source)
		=> ModEntry.Instance.ParseManifestsAndRequestUpdateInfo(source);

	public void RequestUpdateInfo()
		=> ModEntry.Instance.ParseManifestsAndRequestUpdateInfo();

	public void RegisterUpdateSource(string sourceKey, IUpdateSource source)
		=> ModEntry.Instance.UpdateSources.Add(sourceKey, source);

	public IUpdateChecksApi.ITokenModSetting MakeTokenSetting(Func<string> title, Func<bool> hasValue, Action<G, IModSettingsApi.IModSettingsRoute> setupAction)
		=> new TokenModSetting { Title = title, HasValue = hasValue, SetupAction = setupAction };
}
