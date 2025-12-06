using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;

namespace Nickel.Legacy;

internal sealed class LegacyModWrapper : Mod
{
	internal readonly IPluginPackage<IModManifest> Package;
	internal readonly IModHelper Helper;
	internal readonly IReadOnlySet<ILegacyManifest> LegacyManifests;
	internal readonly ICustomEventHub EventHub;
	internal readonly LegacyRegistry Registry;

	public LegacyModWrapper(IPluginPackage<IModManifest> package, IReadOnlySet<ILegacyManifest> legacyManifests, LegacyRegistry legacyRegistry, IModHelper helper, ILogger logger)
	{
		this.Package = package;
		this.Helper = helper;
		this.LegacyManifests = legacyManifests;
		this.Registry = legacyRegistry;
		this.EventHub = new LegacyPerModEventHub(legacyRegistry.GlobalEventHub, logger);

		var gameRootFolder = new DirectoryInfo(Directory.GetCurrentDirectory());
		var modRootFolder = new DirectoryInfo(package.PackageRoot.FullName);

		foreach (var manifest in this.LegacyManifests)
		{
			manifest.GameRootFolder = gameRootFolder;
			manifest.ModRootFolder = modRootFolder;
			manifest.Logger = logger;
		}
	}

	public override object? GetApi(IModManifest requestingMod)
	{
		if (this.LegacyManifests.OfType<IApiProviderManifest>().SingleOrDefault() is not { } apiProvider)
			return null;

		var legacyRequestingManifest = this.Registry.LoadedManifests.FirstOrDefault(m => m.Name == requestingMod.UniqueName);
		return apiProvider.GetApi(legacyRequestingManifest ?? new NewToLegacyManifestStub(requestingMod, () => this.Helper.ModRegistry.GetLogger(requestingMod)));
	}
}
