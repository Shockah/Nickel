using System;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Nanoray.PluginManager;

public sealed class PerPluginAssemblyLoadContextProvider<TPluginManifest> : IAssemblyPluginLoaderLoadContextProvider<TPluginManifest>
{
	private Func<IPluginPackage<TPluginManifest>, string> UniqueNameProvider { get; }

	private ConditionalWeakTable<IPluginPackage<TPluginManifest>, WeakReference<AssemblyLoadContext>> Contexts { get; } = [];

	public PerPluginAssemblyLoadContextProvider(Func<IPluginPackage<TPluginManifest>, string> uniqueNameProvider)
	{
		this.UniqueNameProvider = uniqueNameProvider;
	}

	public AssemblyLoadContext GetLoadContext(IPluginPackage<TPluginManifest> package)
	{
		if (this.Contexts.TryGetValue(package, out var weakContext) && weakContext.TryGetTarget(out var context))
			return context;
		context = new(this.UniqueNameProvider(package));
		this.Contexts.AddOrUpdate(package, new WeakReference<AssemblyLoadContext>(context));
		return context;
	}
}
