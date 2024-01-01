using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;
using ILegacyModManifest = CobaltCoreModding.Definitions.ModManifests.IModManifest;

namespace Nickel;

internal sealed class LegacyAssemblyPluginLoader : IPluginLoader<IAssemblyModManifest, Mod>
{
	private IPluginLoader<IAssemblyModManifest, ILegacyModManifest> Loader { get; }
	private Func<IModManifest, IModHelper> HelperProvider { get; }
	private Func<IModManifest, ILogger> LoggerProvider { get; }
	private Func<Assembly> CobaltCoreAssemblyProvider { get; }
	private Func<LegacyDatabase> DatabaseProvider { get; }

	public LegacyAssemblyPluginLoader(
		IPluginLoader<IAssemblyModManifest, ILegacyModManifest> loader,
		Func<IModManifest, IModHelper> helperProvider,
		Func<IModManifest, ILogger> loggerProvider,
		Func<Assembly> cobaltCoreAssemblyProvider,
		Func<LegacyDatabase> databaseProvider
	)
	{
		this.Loader = loader;
		this.HelperProvider = helperProvider;
		this.LoggerProvider = loggerProvider;
		this.CobaltCoreAssemblyProvider = cobaltCoreAssemblyProvider;
		this.DatabaseProvider = databaseProvider;
	}

	public bool CanLoadPlugin(IPluginPackage<IAssemblyModManifest> package)
		=> package.PackageRoot is IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl> && this.Loader.CanLoadPlugin(package);

	public OneOf<Mod, Error<string>> LoadPlugin(IPluginPackage<IAssemblyModManifest> package)
		=> this.Loader.LoadPlugin(package).Match<OneOf<Mod, Error<string>>>(
			mod =>
			{
				var helper = this.HelperProvider(package.Manifest);
				LegacyRegistry registry = new(package.Manifest, helper, this.CobaltCoreAssemblyProvider(), this.DatabaseProvider());
				return new LegacyModWrapper(mod, registry, package.PackageRoot, helper, this.LoggerProvider(package.Manifest));
			},
			error => error
		);

	private sealed class LegacyRegistry
		: IModLoaderContact, IPrelaunchContactPoint,
		ISpriteRegistry, IDeckRegistry, IStatusRegistry, ICardRegistry, IArtifactRegistry,
		IAnimationRegistry, ICharacterRegistry,
		IPartTypeRegistry, IShipPartRegistry, IShipRegistry, IRawShipRegistry, IStartershipRegistry
	{
		public Assembly CobaltCoreAssembly { get; }

		private IModManifest ModManifest { get; }
		private IModHelper Helper { get; }
		private ILogger Logger { get; }

		public IEnumerable<ILegacyManifest> LoadedManifests
		{
			get
			{
				if (this.Helper.ModRegistry is not ModRegistry modRegistry)
					return Enumerable.Empty<ILegacyManifest>();
				return modRegistry.ModUniqueNameToInstance.Values
					.OfType<LegacyModWrapper>()
					.Select(mod => mod.LegacyManifest);
			}
		}

		public Func<object> GetCobaltCoreGraphicsDeviceFunc
			=> () => MG.inst.GraphicsDevice;

		private LegacyDatabase Database { get; }

		public LegacyRegistry(
			IModManifest modManifest,
			IModHelper helper,
			ILogger logger,
			Assembly cobaltCoreAssembly,
			LegacyDatabase database
		)
		{
			this.ModManifest = modManifest;
			this.Helper = helper;
			this.CobaltCoreAssembly = cobaltCoreAssembly;
			this.Database = database;
		}

		public bool RegisterNewAssembly(Assembly assembly, DirectoryInfo working_directory)
			=> throw new NotImplementedException($"This method is not supported in {NickelConstants.Name}");

		public TApi? GetApi<TApi>(string modName) where TApi : class
			=> this.Helper.ModRegistry.GetApi<TApi>(modName);

		public ILegacyManifest LookupManifest(string globalName)
		{
			if (this.Helper.ModRegistry is not ModRegistry modRegistry)
				throw new KeyNotFoundException();
			if (!modRegistry.ModUniqueNameToInstance.TryGetValue(globalName, out var mod))
				throw new KeyNotFoundException();
			if (mod is not LegacyModWrapper legacyModWrapper)
				throw new KeyNotFoundException();
			return legacyModWrapper.LegacyManifest;
		}

		public ExternalGlossary LookupGlossary(string globalName)
			=> throw new NotImplementedException(); // TODO: implement

		public ExternalSprite LookupSprite(string globalName)
			=> this.Database.GetSprite(globalName);

		public bool RegisterArt(ExternalSprite sprite_data, int? overwrite_value = null)
		{
			if (overwrite_value is not null)
				throw new NotImplementedException($"This method is not supported in {NickelConstants.Name}");
			this.Database.RegisterSprite(this.ModManifest, sprite_data);
			return true;
		}

		public ExternalDeck LookupDeck(string globalName)
			=> this.Database.GetDeck(globalName);

		public bool RegisterDeck(ExternalDeck deck, int? overwrite = null)
		{
			if (overwrite is not null)
				throw new NotImplementedException($"This method is not supported in {NickelConstants.Name}");
			this.Database.RegisterDeck(this.ModManifest, deck);
			return true;
		}

		public ExternalCard LookupCard(string globalName)
			=> this.Database.GetCard(globalName);

		public bool RegisterCard(ExternalCard card, string? overwrite = null)
		{
			if (!string.IsNullOrEmpty(overwrite))
				throw new NotImplementedException($"This method is not supported in {NickelConstants.Name}");
			this.Database.RegisterCard(this.ModManifest, card);
			return true;
		}

		public ExternalArtifact LookupArtifact(string globalName)
			=> this.Database.GetArtifact(globalName);

		public bool RegisterArtifact(ExternalArtifact artifact, string? overwrite = null)
		{
			if (!string.IsNullOrEmpty(overwrite))
				throw new NotImplementedException($"This method is not supported in {NickelConstants.Name}");
			this.Database.RegisterArtifact(this.ModManifest, artifact);
			return true;
		}

		public ExternalStatus LookupStatus(string globalName)
			=> this.Database.GetStatus(globalName);

		public bool RegisterStatus(ExternalStatus status)
		{
			this.Database.RegisterStatus(this.ModManifest, status);
			return true;
		}

		public ExternalAnimation LookupAnimation(string globalName)
			=> this.Database.GetAnimation(globalName);

		public bool RegisterAnimation(ExternalAnimation animation)
		{
			this.Database.RegisterAnimation(this.ModManifest, animation);
			return true;
		}

		public ExternalCharacter LookupCharacter(string globalName)
			=> this.Database.GetCharacter(globalName);

		public bool RegisterCharacter(ExternalCharacter character)
		{
			this.Database.RegisterCharacter(this.ModManifest, character);
			return true;
		}

		public ExternalPartType LookupPartType(string globalName)
			=> this.Database.GetPartType(globalName);

		public bool RegisterPartType(ExternalPartType externalPartType)
		{
			this.Database.RegisterPartType(this.ModManifest, externalPartType);
			return true;
		}

		public ExternalPart LookupPart(string globalName)
			=> this.Database.GetPart(globalName);

		public bool RegisterPart(ExternalPart externalPart)
		{
			this.Database.RegisterPart(this.ModManifest, externalPart);
			return true;
		}

		public bool RegisterRawPart(string global_name, int spr_value, int? off_spr_value = null)
		{
			this.Database.RegisterRawPart(this.ModManifest, global_name, spr_value, off_spr_value);
			return true;
		}

		public object LookupShip(string globalName)
			=> this.Database.ActualizeShip(globalName);

		public bool RegisterShip(ExternalShip ship)
		{
			this.Database.RegisterShip(ship);
			return true;
		}

		public bool RegisterShip(object shipObject, string global_name)
		{
			if (shipObject is not Ship ship)
			{
				this.Logger.LogError("Tried to register a new Ship, but the given object {Object} is not a `{ShipTypeName}`.", shipObject, typeof(Ship).FullName);
				return false;
			}

			this.Database.RegisterShip(ship, global_name);
			return true;
		}

		public object LookupStarterShip(string globalName) => throw new NotImplementedException();

		public bool RegisterStartership(ExternalStarterShip starterShip) => throw new NotImplementedException();
	}

	private sealed class LegacyModWrapper : Mod
	{
		internal ILegacyModManifest LegacyManifest { get; }
		private LegacyRegistry LegacyRegistry { get; }

		public LegacyModWrapper(ILegacyModManifest legacyManifest, LegacyRegistry legacyRegistry, IDirectoryInfo directory, IModHelper helper, ILogger logger)
		{
			this.LegacyManifest = legacyManifest;
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
			helper.Events.OnModLoadPhaseFinished += this.FinalizePreparations;

			legacyManifest.GameRootFolder = new DirectoryInfo(Directory.GetCurrentDirectory());
			legacyManifest.ModRootFolder = new DirectoryInfo(directory.FullName);
			legacyManifest.Logger = logger;
		}

		public override object? GetApi(IModManifest requestingMod)
		{
			if (this.LegacyManifest is not IApiProviderManifest apiProvider)
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
			this.LegacyManifest.BootMod(this.LegacyRegistry);
		}

		[EventPriority(-100)]
		private void LoadSpriteManifest(object? sender, ModLoadPhase phase)
		{
			if (phase != ModLoadPhase.AfterGameAssembly)
				return;
			(this.LegacyManifest as ISpriteManifest)?.LoadManifest(this.LegacyRegistry);
		}

		[EventPriority(-300)]
		private void LoadDeckManifest(object? sender, ModLoadPhase phase)
		{
			if (phase != ModLoadPhase.AfterGameAssembly)
				return;
			(this.LegacyManifest as IDeckManifest)?.LoadManifest(this.LegacyRegistry);
		}

		[EventPriority(-400)]
		private void LoadStatusManifest(object? sender, ModLoadPhase phase)
		{
			if (phase != ModLoadPhase.AfterGameAssembly)
				return;
			(this.LegacyManifest as IStatusManifest)?.LoadManifest(this.LegacyRegistry);
		}

		[EventPriority(-500)]
		private void LoadCardManifest(object? sender, ModLoadPhase phase)
		{
			if (phase != ModLoadPhase.AfterGameAssembly)
				return;
			(this.LegacyManifest as ICardManifest)?.LoadManifest(this.LegacyRegistry);
		}

		[EventPriority(-600)]
		private void LoadArtifactManifest(object? sender, ModLoadPhase phase)
		{
			if (phase != ModLoadPhase.AfterGameAssembly)
				return;
			(this.LegacyManifest as IArtifactManifest)?.LoadManifest(this.LegacyRegistry);
		}

		[EventPriority(-700)]
		private void LoadAnimationManifest(object? sender, ModLoadPhase phase)
		{
			if (phase != ModLoadPhase.AfterGameAssembly)
				return;
			(this.LegacyManifest as IAnimationManifest)?.LoadManifest(this.LegacyRegistry);
		}

		[EventPriority(-800)]
		private void LoadCharacterManifest(object? sender, ModLoadPhase phase)
		{
			if (phase != ModLoadPhase.AfterGameAssembly)
				return;
			(this.LegacyManifest as ICharacterManifest)?.LoadManifest(this.LegacyRegistry);
		}

		[EventPriority(-900)]
		private void LoadPartTypeManifest(object? sender, ModLoadPhase phase)
		{
			if (phase != ModLoadPhase.AfterGameAssembly)
				return;
			(this.LegacyManifest as IPartTypeManifest)?.LoadManifest(this.LegacyRegistry);
		}

		[EventPriority(-1000)]
		private void LoadShipPartManifest(object? sender, ModLoadPhase phase)
		{
			if (phase != ModLoadPhase.AfterGameAssembly)
				return;
			(this.LegacyManifest as IShipPartManifest)?.LoadManifest(this.LegacyRegistry);
		}

		[EventPriority(-10000)]
		private void FinalizePreparations(object? sender, ModLoadPhase phase)
		{
			if (phase != ModLoadPhase.AfterGameAssembly)
				return;
			(this.LegacyManifest as IPrelaunchManifest)?.FinalizePreperations(this.LegacyRegistry);
		}
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
