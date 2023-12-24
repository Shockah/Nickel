using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;
using ILegacyModManifest = CobaltCoreModding.Definitions.ModManifests.IModManifest;

namespace Nickel;

internal sealed class LegacyAssemblyPluginLoader<TContactPoint> : IPluginLoader<IAssemblyModManifest, Mod>
{
    private IPluginLoader<IAssemblyModManifest, ILegacyModManifest> Loader { get; init; }
    private Func<IModManifest, IModHelper> HelperProvider { get; init; }

    public LegacyAssemblyPluginLoader(
        IPluginLoader<IAssemblyModManifest, ILegacyModManifest> loader,
        Func<IModManifest, IModHelper> helperProvider
    )
    {
        this.Loader = loader;
        this.HelperProvider = helperProvider;
    }

    public bool CanLoadPlugin(IPluginPackage<IAssemblyModManifest> package)
        => Loader.CanLoadPlugin(package);

    public OneOf<Mod, Error<string>> LoadPlugin(IPluginPackage<IAssemblyModManifest> package)
        => this.Loader.LoadPlugin(package).Match<OneOf<Mod, Error<string>>>(
            mod => new LegacyModWrapper(mod, this.HelperProvider(package.Manifest)),
            error => error
        );

    private sealed class LegacyModWrapper : Mod, IModLoaderContact, IPrelaunchContactPoint
    {
        private ILegacyModManifest LegacyManifest { get; init; }

        public IEnumerable<ILegacyManifest> LoadedManifests => throw new NotImplementedException();

        public Assembly CobaltCoreAssembly => throw new NotImplementedException();

        public LegacyModWrapper(ILegacyModManifest legacyManifest, IModHelper helper)
        {
            this.LegacyManifest = legacyManifest;
            helper.Events.OnModLoadPhaseFinished += OnEarlyModLoadPhaseFinished;
            helper.Events.OnModLoadPhaseFinished += OnLateModLoadPhaseFinished;
        }

        [EventPriority(0)]
        private void OnEarlyModLoadPhaseFinished(object? sender, ModLoadPhase phase)
            => this.LegacyManifest.BootMod(this);

        [EventPriority(-1000)]
        private void OnLateModLoadPhaseFinished(object? sender, ModLoadPhase phase)
            => (this.LegacyManifest as IPrelaunchManifest)?.FinalizePreperations(this);

        public bool RegisterNewAssembly(Assembly assembly, DirectoryInfo working_directory)
        {
            // TODO: implement or keep as throwing
            throw new NotImplementedException();
        }

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
    }
}
