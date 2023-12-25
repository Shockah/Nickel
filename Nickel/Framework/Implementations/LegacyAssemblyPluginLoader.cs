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
    private Func<SpriteManager> SpriteManagerProvider { get; init; }
    private Func<DeckManager> DeckManagerProvider { get; init; }

    public LegacyAssemblyPluginLoader(
        IPluginLoader<IAssemblyModManifest, ILegacyModManifest> loader,
        Func<IModManifest, IModHelper> helperProvider,
        Func<IModManifest, ILogger> loggerProvider,
        Func<Assembly> cobaltCoreAssemblyProvider,
        Func<SpriteManager> spriteManagerProvider,
        Func<DeckManager> deckManagerProvider
    )
    {
        this.Loader = loader;
        this.HelperProvider = helperProvider;
        this.LoggerProvider = loggerProvider;
        this.CobaltCoreAssemblyProvider = cobaltCoreAssemblyProvider;
        this.SpriteManagerProvider = spriteManagerProvider;
        this.DeckManagerProvider = deckManagerProvider;
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
                LegacyRegistry registry = new(package.Manifest, helper, this.CobaltCoreAssemblyProvider(), this.SpriteManagerProvider, this.DeckManagerProvider);
                return new LegacyModWrapper(mod, registry, directoryPackage.Directory, helper, this.LoggerProvider(package.Manifest));
            },
            error => error
        );
    }

    private sealed class LegacyRegistry : IModLoaderContact, IPrelaunchContactPoint, ISpriteRegistry, IDeckRegistry
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

        private Func<SpriteManager> SpriteManagerProvider { get; init; }
        private Func<DeckManager> DeckManagerProvider { get; init; }

        public LegacyRegistry(
            IModManifest modManifest,
            IModHelper helper,
            Assembly cobaltCoreAssembly,
            Func<SpriteManager> spriteManagerProvider,
            Func<DeckManager> deckManagerProvider
        )
        {
            this.ModManifest = modManifest;
            this.Helper = helper;
            this.CobaltCoreAssembly = cobaltCoreAssembly;
            this.SpriteManagerProvider = spriteManagerProvider;
            this.DeckManagerProvider = deckManagerProvider;
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
        {
            if (!this.SpriteManagerProvider().TryGetByUniqueName(globalName, out var entry) || entry.Legacy is not { } legacy)
                throw new KeyNotFoundException();
            return legacy;
        }

        public bool RegisterArt(ExternalSprite sprite_data, int? overwrite_value = null)
        {
            Func<Stream> GetStreamProvider()
            {
                if (sprite_data.virtual_location is { } provider)
                    return provider;
                if (sprite_data.physical_location is { } path)
                    return () => path.OpenRead().ToMemoryStream();
                throw new ArgumentException("Unsupported ExternalSprite");
            }

            var entry = this.SpriteManagerProvider().RegisterSpriteWithUniqueName(this.ModManifest, sprite_data, sprite_data.GlobalName, GetStreamProvider());
            sprite_data.Id = (int)entry.Sprite;
            return true;
        }

        public ExternalDeck LookupDeck(string globalName)
        {
            if (!this.DeckManagerProvider().TryGetByUniqueName(globalName, out var entry) || entry.Legacy is not { } legacy)
                throw new KeyNotFoundException();
            return legacy;
        }

        public bool RegisterDeck(ExternalDeck deck, int? overwrite = null)
        {
            DeckDef definition = new() { color = new((uint)deck.DeckColor.ToArgb()), titleColor = new((uint)deck.TitleColor.ToArgb()) };
            var entry = this.DeckManagerProvider().RegisterDeckWithUniqueName(this.ModManifest, deck, deck.GlobalName, definition);
            deck.Id = (int)entry.Deck;
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

        [EventPriority(-1000)]
        private void FinalizePreparations(object? sender, ModLoadPhase phase)
        {
            if (phase != ModLoadPhase.AfterGameAssembly)
                return;
            (this.LegacyManifest as IPrelaunchManifest)?.FinalizePreperations(this.LegacyRegistry);
        }
    }
}
