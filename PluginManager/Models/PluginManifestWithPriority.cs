using System.Numerics;

namespace Nanoray.PluginManager;

/// <summary>
/// Describes a plugin manifest with a set priority.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPriority">The priority comparable type.</typeparam>
public record struct PluginManifestWithPriority<TPluginManifest, TPriority>
(
	TPluginManifest Manifest,
	TPriority Priority
) where TPriority : struct, INumber<TPriority>;
