using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    private LegacyDatabase Database { get; init; }

    public LegacyAssemblyPluginLoader(
        IPluginLoader<IAssemblyModManifest, ILegacyModManifest> loader,
        Func<IModManifest, IModHelper> helperProvider,
        Func<IModManifest, ILogger> loggerProvider,
        Func<Assembly> cobaltCoreAssemblyProvider,
        Func<ContentManager> contentManagerProvider
    )
    {
        this.Loader = loader;
        this.HelperProvider = helperProvider;
        this.LoggerProvider = loggerProvider;
        this.CobaltCoreAssemblyProvider = cobaltCoreAssemblyProvider;
        this.Database = new(contentManagerProvider);
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
                LegacyRegistry registry = new(package.Manifest, helper, this.CobaltCoreAssemblyProvider(), this.Database);
                return new LegacyModWrapper(mod, registry, directoryPackage.Directory, helper, this.LoggerProvider(package.Manifest));
            },
            error => error
        );
    }

    private sealed class LegacyDatabase
    {
        private Func<ContentManager> ContentManagerProvider { get; init; }

        private Dictionary<string, ExternalSprite> GlobalNameToSprite { get; init; } = new();
        private Dictionary<string, ExternalDeck> GlobalNameToDeck { get; init; } = new();
        private Dictionary<string, ExternalCard> GlobalNameToCard { get; init; } = new();

        public LegacyDatabase(Func<ContentManager> contentManagerProvider)
        {
            this.ContentManagerProvider = contentManagerProvider;
        }

        public void RegisterSprite(IModManifest mod, ExternalSprite value)
        {
            Func<Stream> GetStreamProvider()
            {
                if (value.virtual_location is { } provider)
                    return provider;
                if (value.physical_location is { } path)
                    return () => path.OpenRead().ToMemoryStream();
                throw new ArgumentException("Unsupported ExternalSprite");
            }

            var entry = this.ContentManagerProvider().Sprites.RegisterSprite(mod, value.GlobalName, GetStreamProvider());
            value.Id = (int)entry.Sprite;
            this.GlobalNameToSprite[value.GlobalName] = value;
        }

        public void RegisterDeck(IModManifest mod, ExternalDeck value)
        {
            DeckConfiguration configuration = new()
            {
                Definition = new() { color = new((uint)value.DeckColor.ToArgb()), titleColor = new((uint)value.TitleColor.ToArgb()) },
                DefaultCardArt = (Spr)value.CardArtDefault.Id!.Value,
                BorderSprite = (Spr)value.BorderSprite.Id!.Value,
                OverBordersSprite = value.BordersOverSprite is null ? null : (Spr)value.BordersOverSprite.Id!.Value
            };
            var entry = this.ContentManagerProvider().Decks.RegisterDeck(mod, value.GlobalName, configuration);
            value.Id = (int)entry.Deck;
            this.GlobalNameToDeck[value.GlobalName] = value;
        }

        public void RegisterCard(IModManifest mod, ExternalCard value)
        {
            CardConfiguration configuration = new()
            {
                CardType = value.CardType,
                Meta = new CardMeta() { deck = value.ActualDeck?.Id is int deckId ? (Deck)deckId : Deck.colorless },
                Art = (Spr)value.CardArt.Id!.Value
            };
            this.ContentManagerProvider().Cards.RegisterCard(mod, value.GlobalName, configuration);
            this.GlobalNameToCard[value.GlobalName] = value;
        }

        public ExternalSprite GetSprite(string globalName)
            => this.GlobalNameToSprite.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

        public ExternalDeck GetDeck(string globalName)
            => this.GlobalNameToDeck.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();

        public ExternalCard GetCard(string globalName)
            => this.GlobalNameToCard.TryGetValue(globalName, out var value) ? value : throw new KeyNotFoundException();
    }

    private sealed class LegacyRegistry : IModLoaderContact, IPrelaunchContactPoint, ISpriteRegistry, IDeckRegistry, ICardRegistry
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
            => throw new NotImplementedException($"This method is not supported in {typeof(Nickel).Namespace!}");

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

        public ExternalSprite LookupSprite(string globalName)
            => this.Database.GetSprite(globalName);

        public bool RegisterArt(ExternalSprite sprite_data, int? overwrite_value = null)
        {
            if (overwrite_value is not null)
                throw new NotImplementedException($"This method is not supported in {typeof(Nickel).Namespace!}");
            this.Database.RegisterSprite(this.ModManifest, sprite_data);
            return true;
        }

        public ExternalDeck LookupDeck(string globalName)
            => this.Database.GetDeck(globalName);

        public bool RegisterDeck(ExternalDeck deck, int? overwrite = null)
        {
            if (overwrite is not null)
                throw new NotImplementedException($"This method is not supported in {typeof(Nickel).Namespace!}");
            this.Database.RegisterDeck(this.ModManifest, deck);
            return true;
        }

        public ExternalCard LookupCard(string globalName)
            => this.Database.GetCard(globalName);

        public bool RegisterCard(ExternalCard card, string? overwrite = null)
        {
            if (!string.IsNullOrEmpty(overwrite))
                throw new NotImplementedException($"This method is not supported in {typeof(Nickel).Namespace!}");
            this.Database.RegisterCard(this.ModManifest, card);
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
            helper.Events.OnModLoadPhaseFinished += LoadCardManifest;
            helper.Events.OnModLoadPhaseFinished += FinalizePreparations;

            legacyManifest.GameRootFolder = new DirectoryInfo(Directory.GetCurrentDirectory());
            legacyManifest.ModRootFolder = directory;
            legacyManifest.Logger = logger;
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
        private void LoadCardManifest(object? sender, ModLoadPhase phase)
        {
            if (phase != ModLoadPhase.AfterGameAssembly)
                return;
            (this.LegacyManifest as ICardManifest)?.LoadManifest(this.LegacyRegistry);
        }

        [EventPriority(-1000)]
        private void FinalizePreparations(object? sender, ModLoadPhase phase)
        {
            if (phase != ModLoadPhase.AfterGameAssembly)
                return;
            (this.LegacyManifest as IPrelaunchManifest)?.FinalizePreperations(this.LegacyRegistry);
        }
    }
}
