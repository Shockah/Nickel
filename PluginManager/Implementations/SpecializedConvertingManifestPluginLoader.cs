using OneOf;
using OneOf.Types;
using System;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginLoader{TPluginManifest,TPlugin}"/> which first converts the plugin manifest to the specialized manifest subclass.
/// </summary>
/// <typeparam name="TSpecializedPluginManifest">The specialized type of the plugin manifest.</typeparam>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPlugin">The plugin type.</typeparam>
public sealed class SpecializedConvertingManifestPluginLoader<TSpecializedPluginManifest, TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
	where TSpecializedPluginManifest : TPluginManifest
{
	private readonly IPluginLoader<TSpecializedPluginManifest, TPlugin> Loader;
	private readonly Func<TPluginManifest, OneOf<TSpecializedPluginManifest, Error<string>>> Converter;

	/// <summary>
	/// Creates a new <see cref="SpecializedConvertingManifestPluginLoader{TSpecializedPluginManifest,TPluginManifest,TPlugin}"/>.
	/// </summary>
	/// <param name="loader">The underlying plugin loader.</param>
	/// <param name="converter">A function that converts a plugin manifest to the specialized manifest subclass.</param>
	public SpecializedConvertingManifestPluginLoader(
		IPluginLoader<TSpecializedPluginManifest, TPlugin> loader,
		Func<TPluginManifest, OneOf<TSpecializedPluginManifest, Error<string>>> converter
	)
	{
		this.Loader = loader;
		this.Converter = converter;
	}

	/// <inheritdoc/>
	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		var specializedManifestOrError = this.Converter(package.Manifest);
		if (specializedManifestOrError.TryPickT1(out var error, out var specializedManifest))
			return error;
		var specializedPackage = new SpecializedPluginPackage(package, specializedManifest);
		return this.Loader.CanLoadPlugin(specializedPackage);
	}

	/// <inheritdoc/>
	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		var specializedManifestOrError = this.Converter(package.Manifest);
		if (specializedManifestOrError.TryPickT1(out var error, out var specializedManifest))
			return error;
		var specializedPackage = new SpecializedPluginPackage(package, specializedManifest);
		return this.Loader.LoadPlugin(specializedPackage);
	}

	private sealed class SpecializedPluginPackage(IPluginPackage<TPluginManifest> package, TSpecializedPluginManifest manifest) : IPluginPackage<TSpecializedPluginManifest>
	{
		public TSpecializedPluginManifest Manifest { get; } = manifest;

		public IDirectoryInfo PackageRoot
			=> this.Package.PackageRoot;

		private IPluginPackage<TPluginManifest> Package { get; } = package;

		public override string ToString()
			=> $"SpecializedPluginPackage {{ {this.Package} }}";

		public void Dispose()
			=> this.Package.Dispose();
	}
}
