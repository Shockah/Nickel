using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;

namespace Nickel.Bugfixes;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger, ExtendableAssemblyDefinitionEditor extendableAssemblyDefinitionEditor) : base(package, helper, logger)
	{
		Instance = this;
		
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
		ExhaustEntireHandActionFixes.ApplyPatches(harmony);
		IsaacUnlockFixes.ApplyPatches(harmony);
		RockFactoryFixes.ApplyPatches(harmony);
		RunSummaryCardOrderFixes.ApplyPatches(harmony);
		SecondOpinionsFixes.ApplyPatches(harmony);
		SurviveVulnerabilityFixes.ApplyPatches(harmony);
		
		SpriteCulling.ApplyPatches(harmony);
	}
}
