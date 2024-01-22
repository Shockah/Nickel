using OneOf;
using OneOf.Types;
using System;

namespace Nanoray.PluginManager;

public sealed class SpecializedConvertingManifestPluginLoader<TSpecializedPluginManifest, TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
	where TSpecializedPluginManifest : TPluginManifest
{
	private IPluginLoader<TSpecializedPluginManifest, TPlugin> Loader { get; }
	private Func<TPluginManifest, OneOf<TSpecializedPluginManifest, Error<string>>> Converter { get; }

	public SpecializedConvertingManifestPluginLoader(
		IPluginLoader<TSpecializedPluginManifest, TPlugin> loader,
		Func<TPluginManifest, OneOf<TSpecializedPluginManifest, Error<string>>> converter
	)
	{
		this.Loader = loader;
		this.Converter = converter;
	}

	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		var specializedManifestOrError = this.Converter(package.Manifest);
		if (specializedManifestOrError.TryPickT1(out var error, out var specializedManifest))
			return error;
		var specializedPackage = new SpecializedPluginPackage(package, specializedManifest);
		return this.Loader.CanLoadPlugin(specializedPackage);
	}

	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		var specializedManifestOrError = this.Converter(package.Manifest);
		if (specializedManifestOrError.TryPickT1(out var error, out var specializedManifest))
			return error;
		var specializedPackage = new SpecializedPluginPackage(package, specializedManifest);
		return this.Loader.LoadPlugin(specializedPackage);
	}

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

		public override string ToString()
			=> $"SpecializedPluginPackage {{ {this.Package} }}";

		public void Dispose()
			=> this.Package.Dispose();
	}
}
