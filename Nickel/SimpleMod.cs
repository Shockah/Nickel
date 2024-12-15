using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;

namespace Nickel;

/// <summary>
/// Describes a simple mod, with a predefined constructor and some properties.
/// </summary>
[PublicAPI]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors)]
public abstract class SimpleMod : Mod
{
	/// <summary>The mod's package.</summary>
	public IPluginPackage<IModManifest> Package { get; }

	/// <summary>A mod-specific helper, giving access to most of the mod loader's API.</summary>
	public IModHelper Helper { get; }
	
	/// <summary>A mod-specific logger.</summary>
	public ILogger Logger { get; }

	/// <summary>
	/// Constructs a new <see cref="SimpleMod"/> instance.
	/// </summary>
	/// <param name="package">The mod's package.</param>
	/// <param name="helper">A mod-specific helper, giving access to most of the mod loader's API.</param>
	/// <param name="logger">A mod-specific logger.</param>
	protected SimpleMod(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger)
	{
		this.Package = package;
		this.Helper = helper;
		this.Logger = logger;
	}
}
