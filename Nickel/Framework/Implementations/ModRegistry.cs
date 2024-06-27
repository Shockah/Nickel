using Nanoray.Pintail;
using Nanoray.PluginManager;
using Nickel.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Nickel;

internal sealed class ModRegistry : IModRegistry
{
	public IModManifest VanillaModManifest
	{
		get
		{
			if (this.VanillaModManifestProvider() is not { } manifest)
				throw new InvalidOperationException("Cannot access game manifest before the game assembly is loaded.");
			return manifest;
		}
	}

	public IModManifest ModLoaderModManifest
		=> this.ModLoaderModManifestProvider();

	public IReadOnlyDictionary<string, IModManifest> ResolvedMods
		=> this.ResolvedModPackages
			.ToDictionary(m => m.Manifest.UniqueName, m => m.Manifest);

	public IReadOnlyDictionary<string, IModManifest> LoadedMods
		=> this.ModUniqueNameToPackage
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Manifest);

	public DirectoryInfo ModsDirectory { get; }

	private readonly IModManifest ModManifest;
	private readonly IReadOnlyDictionary<string, Mod> ModUniqueNameToInstance;
	private readonly IReadOnlyDictionary<string, IPluginPackage<IModManifest>> ModUniqueNameToPackage;
	private readonly IReadOnlyList<IPluginPackage<IModManifest>> ResolvedModPackages;
	private readonly IProxyManager<string> ProxyManager;
	private readonly Func<IModManifest?> VanillaModManifestProvider;
	private readonly Func<IModManifest> ModLoaderModManifestProvider;
	private readonly Dictionary<string, object?> ApiCache = [];

	public ModRegistry(
		IModManifest modManifest,
		Func<IModManifest?> vanillaModManifestProvider,
		Func<IModManifest> modLoaderModManifestProvider,
		DirectoryInfo modsDirectory,
		IReadOnlyDictionary<string, Mod> modUniqueNameToInstance,
		IReadOnlyDictionary<string, IPluginPackage<IModManifest>> modUniqueNameToPackage,
		IReadOnlyList<IPluginPackage<IModManifest>> resolvedModPackages,
		IProxyManager<string> proxyManager
	)
	{
		this.ModManifest = modManifest;
		this.VanillaModManifestProvider = vanillaModManifestProvider;
		this.ModLoaderModManifestProvider = modLoaderModManifestProvider;
		this.ModsDirectory = modsDirectory;
		this.ModUniqueNameToInstance = modUniqueNameToInstance;
		this.ModUniqueNameToPackage = modUniqueNameToPackage;
		this.ResolvedModPackages = resolvedModPackages;
		this.ProxyManager = proxyManager;
	}

	public bool TryProxy<TProxy>(object @object, [MaybeNullWhen(false)] out TProxy proxy) where TProxy : class
		=> this.ProxyManager.TryProxy(@object, "TryProxy", this.ModManifest.UniqueName, out proxy);

	public TProxy? AsProxy<TProxy>(object? @object) where TProxy : class
		=> @object is not null && this.TryProxy<TProxy>(@object, out var proxy) ? proxy : null;

	public TApi? GetApi<TApi>(string uniqueName, SemanticVersion? minimumVersion = null) where TApi : class
	{
		if (!typeof(TApi).IsInterface)
			throw new ArgumentException($"The requested API type {typeof(TApi)} is not an interface.");
		if (!this.ModUniqueNameToInstance.TryGetValue(uniqueName, out var mod))
			return null;
		if (!this.ModUniqueNameToPackage.TryGetValue(uniqueName, out var package))
			return null;
		if (minimumVersion is not null && minimumVersion > package.Manifest.Version)
			return null;

		if (!this.ApiCache.TryGetValue(uniqueName, out var apiObject))
		{
			apiObject = mod.GetApi(this.ModManifest);
			this.ApiCache[uniqueName] = apiObject;
		}
		if (apiObject is null)
			throw new ArgumentException($"The mod {uniqueName} does not expose an API.");

		return this.ProxyManager.ObtainProxy<string, TApi>(apiObject, uniqueName, this.ModManifest.UniqueName);
	}
}
