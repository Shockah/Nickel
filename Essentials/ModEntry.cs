using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace Nickel.Essentials;

public sealed class ModEntry : Mod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal IModManifest Manifest { get; }
	internal ILogger Logger { get; }

	public ModEntry(IModManifest manifest, ILogger logger)
	{
		Instance = this;
		this.Manifest = manifest;
		this.Logger = logger;

		Harmony harmony = new(manifest.UniqueName);
		CrewSelection.ApplyPatches(harmony);
	}
}
