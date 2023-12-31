using OneOf;
using OneOf.Types;
using System;

namespace Nanoray.PluginManager;

public sealed class SpecializedConvertingManifestPluginLoader<TSpecializedPluginManifest, TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
	where TSpecializedPluginManifest : TPluginManifest
{
	private IPluginLoader<TSpecializedPluginManifest, TPlugin> PluginLoader { get; }
	private Func<TPluginManifest, TSpecializedPluginManifest?> Converter { get; }

	public SpecializedConvertingManifestPluginLoader(
		IPluginLoader<TSpecializedPluginManifest, TPlugin> pluginLoader,
		Func<TPluginManifest, TSpecializedPluginManifest?> converter
	)
	{
		this.PluginLoader = pluginLoader;
		this.Converter = converter;
	}

	public bool CanLoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		var specializedManifest = this.Converter(package.Manifest);
		if (specializedManifest is null)
			return false;
		var specializedPackage = MakeSpecializedPackage(package, specializedManifest);
		return this.PluginLoader.CanLoadPlugin(specializedPackage);
	}

	public OneOf<TPlugin, Error<string>> LoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		var specializedManifest = this.Converter(package.Manifest) ?? throw new ArgumentException($"This plugin loader cannot load the plugin package {package}.");
		var specializedPackage = MakeSpecializedPackage(package, specializedManifest);
		return this.PluginLoader.LoadPlugin(specializedPackage);
	}

	private static IPluginPackage<TSpecializedPluginManifest> MakeSpecializedPackage(IPluginPackage<TPluginManifest> package, TSpecializedPluginManifest manifest)
		=> new SpecializedPluginPackage(package, manifest);

	private sealed class SpecializedPluginPackage : IPluginPackage<TSpecializedPluginManifest>
	{
		public TSpecializedPluginManifest Manifest { get; }

		public IDirectoryInfo PackageRoot
			=> this.Package.PackageRoot;

		private IPluginPackage<TPluginManifest> Package { get; }

		public SpecializedPluginPackage(IPluginPackage<TPluginManifest> package, TSpecializedPluginManifest manifest)
		{
			this.Package = package;
			this.Manifest = manifest;
		}

		public void Dispose()
			=> this.Package.Dispose();
	}
}
