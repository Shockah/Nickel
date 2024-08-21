using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using System;

namespace Nickel.Legacy;

internal sealed class CallbackPluginLoader<TPluginManifest, TPlugin>(
	IPluginLoader<TPluginManifest, TPlugin> loader,
	Action<PluginLoadResult<TPlugin>> callback
) : IPluginLoader<TPluginManifest, TPlugin>
{
	public CallbackPluginLoader(
		IPluginLoader<TPluginManifest, TPlugin> loader,
		Action<TPlugin> callback
	) : this(loader, r =>
	{
		if (r.TryPickT0(out var result, out _))
			callback(result.Plugin);
	})
	{ }

	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
		=> loader.CanLoadPlugin(package);

	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		var result = loader.LoadPlugin(package);
		callback(result);
		return result;
	}
}
