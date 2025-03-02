using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;

namespace Nickel.Bugfixes;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;

		var harmony = helper.Utilities.Harmony;
		
		ArtifactCodexFixes.ApplyPatches(harmony);
		CardActionRenderingFixes.ApplyPatches(harmony);
		CardBrowseOrderFixes.ApplyPatches(harmony);
		CardCodexCacheFixes.ApplyPatches(harmony);
		DebugMenuFixes.ApplyPatches(harmony);
		ExhaustEntireHandActionFixes.ApplyPatches(harmony);
		IsaacUnlockFixes.ApplyPatches(harmony);
		RockFactoryFixes.ApplyPatches(harmony);
		RunSummaryCardOrderFixes.ApplyPatches(harmony);
		SecondOpinionsFixes.ApplyPatches(harmony);
		SurviveVulnerabilityFixes.ApplyPatches(harmony);
		
		SpriteCulling.ApplyPatches(harmony);
	}
}
