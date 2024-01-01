using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;

namespace Nickel;

public abstract class SimpleMod : Mod
{
	public IPluginPackage<IModManifest> Package { get; }
	public IModHelper Helper { get; }
	public ILogger Logger { get; }

	public SimpleMod(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger)
	{
		this.Package = package;
		this.Helper = helper;
		this.Logger = logger;
	}
}
