using System;
using System.Collections.Generic;
using System.Linq;
using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager.Implementations;

public sealed class ValidatingPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IPluginPackageResolver<TPluginManifest> Resolver { get; }
	private Func<IPluginPackage<TPluginManifest>, Error<string>?> Validator { get; }

	public ValidatingPluginPackageResolver(IPluginPackageResolver<TPluginManifest> resolver, Func<IPluginPackage<TPluginManifest>, Error<string>?> validator)
	{
		this.Resolver = resolver;
		this.Validator = validator;
	}

	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
		=> this.Resolver.ResolvePluginPackages().Select(packageOrError =>
		{
			if (packageOrError.TryPickT1(out var error, out var package))
				return error;
			if (this.Validator(package) is { } validationError)
				return validationError;
			return OneOf<IPluginPackage<TPluginManifest>, Error<string>>.FromT0(package);
		});
}
