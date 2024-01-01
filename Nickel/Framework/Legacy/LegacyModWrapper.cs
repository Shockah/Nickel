using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModManifests;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;
using ILegacyModManifest = CobaltCoreModding.Definitions.ModManifests.IModManifest;

namespace Nickel.Framework;

internal sealed class LegacyModWrapper : Mod
{
	internal IReadOnlySet<ILegacyManifest> LegacyManifests { get; }
	private LegacyRegistry LegacyRegistry { get; }

	public LegacyModWrapper(IReadOnlySet<ILegacyManifest> legacyManifests, LegacyRegistry legacyRegistry, IDirectoryInfo directory, IModHelper helper, ILogger logger)
	{
		this.LegacyManifests = legacyManifests;
		this.LegacyRegistry = legacyRegistry;
		helper.Events.OnModLoadPhaseFinished += this.BootMod;
		helper.Events.OnModLoadPhaseFinished += this.LoadSpriteManifest;
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

		var legacyRequestingManifest = this.LegacyRegistry.LoadedManifests.FirstOrDefault(m => m.Name == requestingMod.UniqueName);
		if (legacyRequestingManifest is not null)
			return apiProvider.GetApi(legacyRequestingManifest);

		return new NewToLegacyManifestStub(requestingMod);
	}

	[EventPriority(0)]
	private void BootMod(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is ILegacyModManifest modManifest)
				modManifest.BootMod(this.LegacyRegistry);
	}

	[EventPriority(-100)]
	private void LoadSpriteManifest(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is ISpriteManifest modManifest)
				modManifest.LoadManifest(this.LegacyRegistry);
	}

	[EventPriority(-300)]
	private void LoadDeckManifest(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IDeckManifest modManifest)
				modManifest.LoadManifest(this.LegacyRegistry);
	}

	[EventPriority(-400)]
	private void LoadStatusManifest(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IStatusManifest modManifest)
				modManifest.LoadManifest(this.LegacyRegistry);
	}

	[EventPriority(-500)]
	private void LoadCardManifest(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is ICardManifest modManifest)
				modManifest.LoadManifest(this.LegacyRegistry);
	}

	[EventPriority(-600)]
	private void LoadArtifactManifest(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IArtifactManifest modManifest)
				modManifest.LoadManifest(this.LegacyRegistry);
	}

	[EventPriority(-700)]
	private void LoadAnimationManifest(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IAnimationManifest modManifest)
				modManifest.LoadManifest(this.LegacyRegistry);
	}

	[EventPriority(-800)]
	private void LoadCharacterManifest(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is ICharacterManifest modManifest)
				modManifest.LoadManifest(this.LegacyRegistry);
	}

	[EventPriority(-900)]
	private void LoadPartTypeManifest(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IPartTypeManifest modManifest)
				modManifest.LoadManifest(this.LegacyRegistry);
	}

	[EventPriority(-1000)]
	private void LoadShipPartManifest(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IShipPartManifest modManifest)
				modManifest.LoadManifest(this.LegacyRegistry);
	}

	[EventPriority(-1100)]
	private void LoadShipManifest(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IShipManifest modManifest)
				modManifest.LoadManifest(this.LegacyRegistry);
	}

	[EventPriority(-1200)]
	private void LoadStarterShipManifest(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IStartershipManifest modManifest)
				modManifest.LoadManifest(this.LegacyRegistry);
	}

	[EventPriority(-10000)]
	private void FinalizePreparations(object? sender, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterGameAssembly)
			return;
		foreach (var manifest in this.LegacyManifests)
			if (manifest is IPrelaunchManifest modManifest)
				modManifest.FinalizePreperations(this.LegacyRegistry);
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
