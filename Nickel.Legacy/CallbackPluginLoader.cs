using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using System;

namespace Nickel.Legacy;

internal sealed class CallbackPluginLoader<TPluginManifest, TPlugin>(
	IPluginLoader<TPluginManifest, TPlugin> loader,
	Action<OneOf<TPlugin, Error<string>>> callback
) : IPluginLoader<TPluginManifest, TPlugin>
{
	private IPluginLoader<TPluginManifest, TPlugin> Loader { get; } = loader;
	private Action<OneOf<TPlugin, Error<string>>> Callback { get; } = callback;

	public CallbackPluginLoader(
		IPluginLoader<TPluginManifest, TPlugin> loader,
		Action<TPlugin> callback
	) : this(loader, r =>
	{
		if (r.TryPickT0(out var plugin, out _))
			callback(plugin);
	})
	{ }

	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.Loader.CanLoadPlugin(package);

	public OneOf<TPlugin, Error<string>> LoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		var result = this.Loader.LoadPlugin(package);
		this.Callback(result);
		return result;
	}
}
