using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;
using ILegacyModManifest = CobaltCoreModding.Definitions.ModManifests.IModManifest;

namespace Nickel;

internal sealed class LegacyModWrapper : Mod
{
	internal IReadOnlySet<ILegacyManifest> LegacyManifests { get; }
	private ICustomEventHub EventHub { get; }
	private LegacyRegistry Registry { get; }

	public LegacyModWrapper(IReadOnlySet<ILegacyManifest> legacyManifests, LegacyRegistry legacyRegistry, IDirectoryInfo directory, IModHelper helper, ILogger logger)
	{
		this.LegacyManifests = legacyManifests;
		this.Registry = legacyRegistry;
		this.EventHub = new LegacyPerModEventHub(legacyRegistry.GlobalEventHub, logger);

		helper.Events.OnModLoadPhaseFinished += this.BootMod;
		helper.Events.OnModLoadPhaseFinished += this.LoadSpriteManifest;
		helper.Events.OnModLoadPhaseFinished += this.LoadGlossaryManifest;
		helper.Events.OnModLoadPhaseFinished += this.LoadDeckManifest;
		helper.Events.OnModLoadPhaseFinished += this.LoadStatusManifest;
		helper.Events.OnModLoadPhaseFinished += this.LoadCardManifest;
		helper.Events.OnModLoadPhaseFinished += this.LoadArtifactManifest;
		helper.Events.OnModLoadPhaseFinished += this.LoadAnimationManifest;
		helper.Events.OnModLoadPhaseFinished += this.LoadCharacterManifest;
		helper.Events.OnModLoadPhaseFinished += this.LoadPartTypeManifest;
		helper.Events.OnModLoadPhaseFinished += this.LoadShipPartManifest;
		helper.Events.OnModLoadPhaseFinished += this.LoadShipManifest;
		helper.Events.OnModLoadPhaseFinished += this.LoadStarterShipManifest;
		helper.Events.OnModLoadPhaseFinished += this.LoadStoryManifests;
		helper.Events.OnModLoadPhaseFinished += this.LoadEventHubManifests;
		helper.Events.OnModLoadPhaseFinished += this.FinalizePreparations;

		DirectoryInfo gameRootFolder = new(Directory.GetCurrentDirectory());
		DirectoryInfo modRootFolder = new(directory.FullName);

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

	[EventPriority(0)]
	private void BootMod(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is ILegacyModManifest modManifest)
				modManifest.BootMod(this.Registry);
	}

	[EventPriority(-100)]
	private void LoadSpriteManifest(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is ISpriteManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-200)]
	private void LoadGlossaryManifest(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IGlossaryManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-300)]
	private void LoadDeckManifest(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IDeckManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-400)]
	private void LoadStatusManifest(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IStatusManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-500)]
	private void LoadCardManifest(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is ICardManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-600)]
	private void LoadArtifactManifest(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IArtifactManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-700)]
	private void LoadAnimationManifest(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IAnimationManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-800)]
	private void LoadCharacterManifest(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is ICharacterManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-900)]
	private void LoadPartTypeManifest(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IPartTypeManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-1000)]
	private void LoadShipPartManifest(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IShipPartManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-1100)]
	private void LoadShipManifest(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IShipManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-1200)]
	private void LoadStarterShipManifest(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IStartershipManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-1300)]
	private void LoadStoryManifests(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IStoryManifest modManifest)
				modManifest.LoadManifest(this.Registry);
	}

	[EventPriority(-1400)]
	private void LoadEventHubManifests(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is ICustomEventManifest modManifest)
				modManifest.LoadManifest(this.EventHub);
	}

	[EventPriority(-10000)]
	private void FinalizePreparations(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IPrelaunchManifest modManifest)
				modManifest.FinalizePreperations(this.Registry);
	}

	private sealed class NewToLegacyManifestStub : ILegacyManifest
	{
		public string Name
			=> this.ModManifest.UniqueName;

		public IEnumerable<DependencyEntry> Dependencies
			=> Enumerable.Empty<DependencyEntry>();

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
