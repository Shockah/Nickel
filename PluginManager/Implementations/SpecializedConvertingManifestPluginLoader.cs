using System;
using System.Collections.Generic;
using System.IO;
using OneOf;
using OneOf.Types;

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
		=> package is IDirectoryPluginPackage<TPluginManifest> directoryPackage
			? new SpecializedDirectoryPluginPackage(directoryPackage, manifest)
			: new SpecializedPluginPackage(package, manifest);

	private sealed class SpecializedPluginPackage : IPluginPackage<TSpecializedPluginManifest>
	{
		public TSpecializedPluginManifest Manifest { get; }

		public IReadOnlySet<string> DataEntries
			=> this.Package.DataEntries;

		private IPluginPackage<TPluginManifest> Package { get; }

		public SpecializedPluginPackage(IPluginPackage<TPluginManifest> package, TSpecializedPluginManifest manifest)
		{
			this.Package = package;
			this.Manifest = manifest;
		}

		public Stream GetDataStream(string entry)
			=> this.Package.GetDataStream(entry);
	}

	private sealed class SpecializedDirectoryPluginPackage : IDirectoryPluginPackage<TSpecializedPluginManifest>
	{
		public TSpecializedPluginManifest Manifest { get; }

		public DirectoryInfo Directory
			=> this.Package.Directory;

		public IReadOnlySet<string> DataEntries
			=> this.Package.DataEntries;

		private IDirectoryPluginPackage<TPluginManifest> Package { get; }

		public SpecializedDirectoryPluginPackage(IDirectoryPluginPackage<TPluginManifest> package, TSpecializedPluginManifest manifest)
		{
			this.Package = package;
			this.Manifest = manifest;
		}

		public Stream GetDataStream(string entry)
			=> this.Package.GetDataStream(entry);
	}
}
