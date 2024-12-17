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
		CardBrowseSortingFixes.ApplyPatches(harmony);
		DebugMenuFixes.ApplyPatches(harmony);
		ExhaustEntireHandActionFixes.ApplyPatches(harmony);
		IsaacUnlockFixes.ApplyPatches(harmony);
		SecondOpinionsFixes.ApplyPatches(harmony);
	}
}
