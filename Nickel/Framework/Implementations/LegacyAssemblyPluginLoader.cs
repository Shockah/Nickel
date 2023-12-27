using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;
using ILegacyModManifest = CobaltCoreModding.Definitions.ModManifests.IModManifest;

namespace Nickel;

internal sealed class LegacyAssemblyPluginLoader : IPluginLoader<IAssemblyModManifest, Mod>
{
    private IPluginLoader<IAssemblyModManifest, ILegacyModManifest> Loader { get; init; }
    private Func<IModManifest, IModHelper> HelperProvider { get; init; }
    private Func<IModManifest, ILogger> LoggerProvider { get; init; }
    private Func<Assembly> CobaltCoreAssemblyProvider { get; init; }
    private Func<LegacyDatabase> DatabaseProvider { get; init; }

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
        => package is IDirectoryPluginPackage<IAssemblyModManifest> && Loader.CanLoadPlugin(package);

    public OneOf<Mod, Error<string>> LoadPlugin(IPluginPackage<IAssemblyModManifest> package)
    {
        if (package is not IDirectoryPluginPackage<IAssemblyModManifest> directoryPackage)
            throw new ArgumentException($"This plugin loader cannot load the plugin package {package}.");
        return this.Loader.LoadPlugin(package).Match<OneOf<Mod, Error<string>>>(
            mod =>
            {
                var helper = this.HelperProvider(package.Manifest);
                LegacyRegistry registry = new(package.Manifest, helper, this.CobaltCoreAssemblyProvider(), this.DatabaseProvider());
                return new LegacyModWrapper(mod, registry, directoryPackage.Directory, helper, this.LoggerProvider(package.Manifest));
            },
            error => error
        );
    }

    private sealed class LegacyRegistry : IModLoaderContact, IPrelaunchContactPoint, ISpriteRegistry, IDeckRegistry, IStatusRegistry, ICardRegistry, IArtifactRegistry
    {
        public Assembly CobaltCoreAssembly { get; init; }

        private IModManifest ModManifest { get; init; }
        private IModHelper Helper { get; init; }

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

        private LegacyDatabase Database { get; init; }

        public LegacyRegistry(
            IModManifest modManifest,
            IModHelper helper,
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
    }

    private sealed class LegacyModWrapper : Mod
    {
        internal ILegacyModManifest LegacyManifest { get; init; }
        private LegacyRegistry LegacyRegistry { get; init; }

        public LegacyModWrapper(ILegacyModManifest legacyManifest, LegacyRegistry legacyRegistry, DirectoryInfo directory, IModHelper helper, ILogger logger)
        {
            this.LegacyManifest = legacyManifest;
            this.LegacyRegistry = legacyRegistry;
            helper.Events.OnModLoadPhaseFinished += BootMod;
            helper.Events.OnModLoadPhaseFinished += LoadSpriteManifest;
            helper.Events.OnModLoadPhaseFinished += LoadDeckManifest;
            helper.Events.OnModLoadPhaseFinished += LoadStatusManifest;
            helper.Events.OnModLoadPhaseFinished += LoadCardManifest;
            helper.Events.OnModLoadPhaseFinished += LoadArtifactManifest;
            helper.Events.OnModLoadPhaseFinished += FinalizePreparations;

            legacyManifest.GameRootFolder = new DirectoryInfo(Directory.GetCurrentDirectory());
            legacyManifest.ModRootFolder = directory;
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

        [EventPriority(-1000)]
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

        private IModManifest ModManifest { get; init; }

        public NewToLegacyManifestStub(IModManifest modManifest)
        {
            this.ModManifest = modManifest;
        }
    }
}
