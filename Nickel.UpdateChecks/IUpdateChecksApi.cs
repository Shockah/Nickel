using Nickel.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel.UpdateChecks;

/// <summary>
/// Provides access to <c>Nickel.UpdateChecks</c> APIs that do not rely on any <c>CobaltCore</c> types.
/// </summary>
public interface IUpdateChecksApi
{
	/// <summary>
	/// Returns the name of the given mod for update purposes.
	/// </summary>
	/// <param name="mod">The mod.</param>
	/// <returns>The name of the given mod for update purposes.</returns>
	string GetModNameForUpdatePurposes(IModManifest mod);
	
	/// <summary>
	/// Attempts to get the retrieved update info for the given mod.
	/// </summary>
	/// <param name="mod">The mod to get the update info for.</param>
	/// <param name="descriptors">The retrieved update descriptors, if succeeded.</param>
	/// <returns>Whether there was any update info for the given mod.</returns>
	/// <remarks>This method returns the update info right away, even if update checks are still in progress. If you want to wait until update checks are done, use <see cref="AwaitUpdateInfo"/> instead.</remarks>
	bool TryGetUpdateInfo(IModManifest mod, [MaybeNullWhen(false)] out List<UpdateDescriptor> descriptors);
	
	/// <summary>
	/// Awaits for the update info for the given mod.
	/// </summary>
	/// <param name="mod">The mod to get the update info for.</param>
	/// <param name="callback">The callback to be invoked when update checks are done.</param>
	/// <remarks>The <see cref="callback"/> will be invoked right away if the update checks are already done by the time this method is called.</remarks>
	void AwaitUpdateInfo(IModManifest mod, Action<IModManifest, List<UpdateDescriptor>> callback);

	IEnumerable<KeyValuePair<string, IUpdateSource>> UpdateSources { get; }

	IUpdateSource? LookupUpdateSourceByKey(string sourceKey);

	/// <summary>
	/// Registers a new update source.
	/// </summary>
	/// <param name="sourceKey">The key this update source will look for in mods' manifest files.</param>
	/// <param name="source">The update source implementation.</param>
	void RegisterUpdateSource(string sourceKey, IUpdateSource source);
	
	/// <summary>
	/// Requests an update check for all resolved mods with only the given update source.
	/// </summary>
	/// <param name="source">The update source.</param>
	/// <remarks>Each update source can ignore such a request and return cached data instead, for example to avoid being rate limited.</remarks>
	void RequestUpdateInfo(IUpdateSource source);
	
	/// <summary>
	/// Requests an update check for all resolved mods, cancelling any currently ongoing checks.
	/// </summary>
	/// <remarks>Each update source can ignore such a request and return cached data instead, for example to avoid being rate limited.</remarks>
	void RequestUpdateInfo();

	SemanticVersion? GetIgnoredUpdateForMod(IModManifest mod);
	
	void SetIgnoredUpdateForMod(IModManifest mod, SemanticVersion? version);

	void SaveSettings();
}
