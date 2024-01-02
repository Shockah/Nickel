using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System.Collections.Generic;

namespace Nickel.Essentials;

public sealed class ModEntry : Mod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal IModManifest Manifest { get; }
	internal ILogger Logger { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

	public ModEntry(IPluginPackage<IModManifest> package, ILogger logger)
	{
		Instance = this;
		this.Manifest = package.Manifest;
		this.Logger = logger;

		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(
				new JsonLocalizationProvider(
					tokenExtractor: new SimpleLocalizationTokenExtractor(),
					localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
				)
			)
		);

		Harmony harmony = new(package.Manifest.UniqueName);
		CrewSelection.ApplyPatches(harmony);
	}
}
