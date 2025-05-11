using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using System.Collections.Generic;

namespace Nickel.Bugfixes;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger, ExtendableAssemblyDefinitionEditor extendableAssemblyDefinitionEditor) : base(package, helper, logger)
	{
		Instance = this;
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(
				new JsonLocalizationProvider(
					tokenExtractor: new SimpleLocalizationTokenExtractor(),
					localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
				)
			)
		);

		extendableAssemblyDefinitionEditor.RegisterDefinitionEditor(new ReboundReagentDefinitionEditor());

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterGameAssembly)
				return;
			this.ProceedAfterGameAssemblyLoaded();
		};
	}

	private void ProceedAfterGameAssemblyLoaded()
	{
		var harmony = this.Helper.Utilities.Harmony;

		ArtifactCodexFixes.ApplyPatches(harmony);
		BlendStateFixes.ApplyPatches(harmony);
		CardActionRenderingFixes.ApplyPatches(harmony);
		CardBrowseOrderFixes.ApplyPatches(harmony);
		CardCodexCacheFixes.ApplyPatches(harmony);
		DebugMenuFixes.ApplyPatches(harmony);
		DisabledActionSpriteFixes.ApplyPatches(harmony);
		EnergyFragmentFixes.ApplyPatches(harmony);
		ExhaustEntireHandActionFixes.ApplyPatches(harmony);
		IsaacUnlockFixes.ApplyPatches(harmony);
		RockFactoryFixes.ApplyPatches(harmony);
		RunSummaryCardOrderFixes.ApplyPatches(harmony);
		SecondOpinionsFixes.ApplyPatches(harmony);
		SurviveVulnerabilityFixes.ApplyPatches(harmony);
		UnimplementedActionFeaturesFixes.ApplyPatches(harmony);

		SpriteCulling.ApplyPatches(harmony);
		SetXRenderer.ApplyPatches(harmony);
	}
}
