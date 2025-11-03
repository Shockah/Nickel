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

	public void AwaitAllUpdateInfo(Action<IReadOnlyDictionary<IModManifest, List<UpdateDescriptor>>> callback)
	{
		var updates = new Dictionary<IModManifest, List<UpdateDescriptor>>();
		
		foreach (var resolvedMod in ModEntry.Instance.Helper.ModRegistry.ResolvedMods.Values)
		{
			if (!this.TryGetUpdateInfo(resolvedMod, out var update))
			{
				ModEntry.Instance.AwaitingUpdateInfo.Add(() => this.AwaitAllUpdateInfo(callback));
				return;
			}

			if (update.Count == 0)
				continue;

			updates[resolvedMod] = update;
		}

		callback(updates);
	}

	public void RequestUpdateInfo(IUpdateSource source)
		=> ModEntry.Instance.ParseManifestsAndRequestUpdateInfo(source);

	public void RequestUpdateInfo()
		=> ModEntry.Instance.ParseManifestsAndRequestUpdateInfo();

	public IEnumerable<KeyValuePair<string, IUpdateSource>> UpdateSources
		=> ModEntry.Instance.UpdateSourceKeyToSource;

	public IUpdateSource? LookupUpdateSourceByKey(string sourceKey)
		=> ModEntry.Instance.UpdateSourceKeyToSource.GetValueOrDefault(sourceKey);

	public void RegisterUpdateSource(string sourceKey, IUpdateSource source)
	{
		ModEntry.Instance.UpdateSourceKeyToSource.Add(sourceKey, source);
		ModEntry.Instance.UpdateSourceToKey.Add(source, sourceKey);
	}

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
