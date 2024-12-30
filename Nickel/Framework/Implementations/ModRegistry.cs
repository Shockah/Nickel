using Nanoray.Pintail;
using Nanoray.PluginManager;
using Nickel.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Nickel;

internal sealed class ModRegistry(
	IModManifest modManifest,
	Func<IModManifest?> vanillaModManifestProvider,
	Func<IModManifest> modLoaderModManifestProvider,
	DirectoryInfo modsDirectory,
	IReadOnlyDictionary<string, Mod> modUniqueNameToInstance,
	IReadOnlyDictionary<string, IPluginPackage<IModManifest>> modUniqueNameToPackage,
	IReadOnlyList<IPluginPackage<IModManifest>> resolvedModPackages,
	IProxyManager<string> proxyManager,
	Func<ModLoadPhaseState> currentModLoadPhaseProvider,
	IModEvents modEvents
) : IModRegistry
{
	public DirectoryInfo ModsDirectory { get; } = modsDirectory;

	private readonly Dictionary<string, object?> ApiCache = [];
	
	public IModManifest VanillaModManifest
	{
		get
		{
			if (vanillaModManifestProvider() is not { } manifest)
				throw new InvalidOperationException("Cannot access game manifest before the game assembly is loaded.");
			return manifest;
		}
	}

	public IModManifest ModLoaderModManifest
		=> modLoaderModManifestProvider();

	public IReadOnlyDictionary<string, IModManifest> ResolvedMods
		=> resolvedModPackages
			.ToDictionary(m => m.Manifest.UniqueName, m => m.Manifest);

	public IReadOnlyDictionary<string, IModManifest> LoadedMods
		=> modUniqueNameToPackage
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Manifest);

	public bool TryProxy<TProxy>(object @object, [MaybeNullWhen(false)] out TProxy proxy) where TProxy : class
		=> proxyManager.TryProxy(@object, "TryProxy", modManifest.UniqueName, out proxy);

	public TProxy? AsProxy<TProxy>(object? @object) where TProxy : class
		=> @object is not null && this.TryProxy<TProxy>(@object, out var proxy) ? proxy : null;

	public TApi? GetApi<TApi>(string uniqueName, SemanticVersion? minimumVersion = null) where TApi : class
	{
		if (!typeof(TApi).IsInterface)
			throw new ArgumentException($"The requested API type {typeof(TApi)} is not an interface.");
		if (!modUniqueNameToInstance.TryGetValue(uniqueName, out var mod))
			return null;
		if (!modUniqueNameToPackage.TryGetValue(uniqueName, out var package))
			return null;
		if (minimumVersion is not null && minimumVersion > package.Manifest.Version)
			return null;

		if (!this.ApiCache.TryGetValue(uniqueName, out var apiObject))
		{
			apiObject = mod.GetApi(modManifest);
			this.ApiCache[uniqueName] = apiObject;
		}
		if (apiObject is null)
			throw new ArgumentException($"The mod {uniqueName} does not expose an API.");

		return proxyManager.ObtainProxy<string, TApi>(apiObject, uniqueName, modManifest.UniqueName);
	}

	public void AwaitApi<TApi>(string uniqueName, Action<TApi> callback) where TApi : class
		=> this.AwaitApi(uniqueName, null, callback);

	public void AwaitApi<TApi>(string uniqueName, SemanticVersion? minimumVersion, Action<TApi> callback) where TApi : class
	{
		if (this.GetApi<TApi>(uniqueName, minimumVersion) is { } api)
		{
			callback(api);
			return;
		}

		if (currentModLoadPhaseProvider() is { Phase: ModLoadPhase.AfterDbInit, IsDone: true })
			return;

		modEvents.OnModLoadPhaseFinished += OnModLoadPhaseFinished;

		void OnModLoadPhaseFinished(object? sender, ModLoadPhase phase)
		{
			modEvents.OnModLoadPhaseFinished -= OnModLoadPhaseFinished;
			this.AwaitApi(uniqueName, minimumVersion, callback);
		}
	}

	public void AwaitApiOrNull<TApi>(string uniqueName, Action<TApi?> callback) where TApi : class
		=> this.AwaitApiOrNull(uniqueName, null, callback);

	public void AwaitApiOrNull<TApi>(string uniqueName, SemanticVersion? minimumVersion, Action<TApi?> callback) where TApi : class
	{
		if (this.GetApi<TApi>(uniqueName, minimumVersion) is { } api)
		{
			callback(api);
			return;
		}

		if (currentModLoadPhaseProvider() is { Phase: ModLoadPhase.AfterDbInit, IsDone: true })
		{
			callback(null);
			return;
		}

		modEvents.OnModLoadPhaseFinished += OnModLoadPhaseFinished;

		void OnModLoadPhaseFinished(object? sender, ModLoadPhase phase)
		{
			modEvents.OnModLoadPhaseFinished -= OnModLoadPhaseFinished;
			this.AwaitApiOrNull(uniqueName, minimumVersion, callback);
		}
	}
}
