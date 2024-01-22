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
	private IPluginLoader<TPluginManifest, TPlugin> Loader { get; } = loader;
	private Action<PluginLoadResult<TPlugin>> Callback { get; } = callback;

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
		=> this.Loader.CanLoadPlugin(package);

	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		var result = this.Loader.LoadPlugin(package);
		this.Callback(result);
		return result;
	}
}
