using OneOf;
using OneOf.Types;
using System;
using System.Linq;

namespace Nanoray.PluginManager;

public sealed class ValidatingPluginLoader<TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
{
	private IPluginLoader<TPluginManifest, TPlugin> Loader { get; }
	private Func<IPluginPackage<TPluginManifest>, TPlugin, ValidatingPluginLoaderResult> Validator { get; }

	public ValidatingPluginLoader(
		IPluginLoader<TPluginManifest, TPlugin> loader,
		Func<IPluginPackage<TPluginManifest>, TPlugin, ValidatingPluginLoaderResult> validator
	)
	{
		this.Loader = loader;
		this.Validator = validator;
	}

	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.Loader.CanLoadPlugin(package);

	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		if (this.Loader.LoadPlugin(package).TryPickT1(out var error, out var loadSuccess))
			return error;

		var validation = this.Validator(package, loadSuccess.Plugin);
		return validation.Match<PluginLoadResult<TPlugin>>(
			validationSuccess => new PluginLoadResult<TPlugin>.Success
			{
				Plugin = loadSuccess.Plugin,
				Warnings = loadSuccess.Warnings.Concat(validationSuccess.Warnings).ToList()
			},
			error => error
		);
	}
}

