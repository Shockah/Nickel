using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;

namespace Nickel;

internal sealed class LegacyModWrapper : Mod
{
	internal IPluginPackage<IModManifest> Package { get; }
	internal IModHelper Helper { get; }
	internal IReadOnlySet<ILegacyManifest> LegacyManifests { get; }
	internal ICustomEventHub EventHub { get; }
	internal LegacyRegistry Registry { get; }

	public LegacyModWrapper(IPluginPackage<IModManifest> package, IReadOnlySet<ILegacyManifest> legacyManifests, LegacyRegistry legacyRegistry, IModHelper helper, ILogger logger)
	{
		this.Package = package;
		this.Helper = helper;
		this.LegacyManifests = legacyManifests;
		this.Registry = legacyRegistry;
		this.EventHub = new LegacyPerModEventHub(legacyRegistry.GlobalEventHub, logger);

		DirectoryInfo gameRootFolder = new(Directory.GetCurrentDirectory());
		DirectoryInfo modRootFolder = new(package.PackageRoot.FullName);

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
		return apiProvider.GetApi(legacyRequestingManifest ?? new NewToLegacyManifestStub(requestingMod));
	}

	private sealed class NewToLegacyManifestStub : ILegacyManifest
	{
		public string Name
			=> this.ModManifest.UniqueName;

		public IEnumerable<DependencyEntry> Dependencies
			=> [];

		public DirectoryInfo? GameRootFolder
		{
			get => null;
			set { }
		}

		public DirectoryInfo? ModRootFolder
		{
			get => null;
			set { }
		}

		public ILogger? Logger
		{
			get => null;
			set { }
		}

		private IModManifest ModManifest { get; }

		public NewToLegacyManifestStub(IModManifest modManifest)
		{
			this.ModManifest = modManifest;
		}
	}
}
