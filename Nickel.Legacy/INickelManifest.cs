using CobaltCoreModding.Definitions.ModManifests;
using Nanoray.PluginManager;

namespace Nickel.Legacy;

/// <summary>
/// Describes a legacy mod which uses Nickel's <see cref="IModHelper"/>.
/// </summary>
public interface INickelManifest : IManifest
{
	/// <summary>
	/// Called when Nickel finishes loading the legacy mod.
	/// </summary>
	/// <param name="package">The mod's package.</param>
	/// <param name="helper">A mod-specific helper, giving access to most of the mod loader's API.</param>
	void OnNickelLoad(IPluginPackage<IModManifest> package, IModHelper helper);
}
