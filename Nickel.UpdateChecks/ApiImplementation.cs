using Nickel.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel.UpdateChecks;

public sealed class ApiImplementation : IUpdateChecksApi
{
	public string GetModNameForUpdatePurposes(IModManifest mod)
		=> ModEntry.GetModNameForUpdatePurposes(mod);
	
	public bool TryGetUpdateInfo(IModManifest mod, [MaybeNullWhen(false)] out List<UpdateDescriptor> descriptors)
		=> ModEntry.Instance.UpdatesAvailable.TryGetValue(mod, out descriptors);

	public void AwaitUpdateInfo(IModManifest mod, Action<IModManifest, List<UpdateDescriptor>> callback)
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

	public IEnumerable<KeyValuePair<string, IUpdateSource>> UpdateSources
		=> ModEntry.Instance.UpdateSources;

	public IUpdateSource? LookupUpdateSourceByKey(string sourceKey)
		=> ModEntry.Instance.UpdateSources.GetValueOrDefault(sourceKey);

	public void RegisterUpdateSource(string sourceKey, IUpdateSource source)
		=> ModEntry.Instance.UpdateSources.Add(sourceKey, source);

	public SemanticVersion? GetIgnoredUpdateForMod(IModManifest mod)
		=> ModEntry.Instance.Settings.IgnoredUpdates.GetValueOrDefault(mod.UniqueName);

	public void SetIgnoredUpdateForMod(IModManifest mod, SemanticVersion? version)
	{
		if (version is null)
			ModEntry.Instance.Settings.IgnoredUpdates.Remove(mod.UniqueName);
		else
			ModEntry.Instance.Settings.IgnoredUpdates[mod.UniqueName] = version.Value;
	}

	public void SaveSettings()
		=> ModEntry.Instance.Helper.Storage.SaveJson(ModEntry.Instance.SettingsFile, ModEntry.Instance.Settings);
}
