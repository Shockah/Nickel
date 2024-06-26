using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager.Implementations;

/// <summary>
/// An <see cref="IPluginPackageResolver{TPluginManifest}"/> that validates whether a plugin package can be resolved before attempting to do so.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public sealed class ValidatingPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IPluginPackageResolver<TPluginManifest> Resolver { get; }
	private Func<IPluginPackage<TPluginManifest>, Error<string>?> Validator { get; }

	/// <summary>
	/// Creates a new <see cref="ValidatingPluginPackageResolver{TPluginManifest}"/>.
	/// </summary>
	/// <param name="resolver">The underlying resolver.</param>
	/// <param name="validator">The validator function.</param>
	public ValidatingPluginPackageResolver(IPluginPackageResolver<TPluginManifest> resolver, Func<IPluginPackage<TPluginManifest>, Error<string>?> validator)
	{
		this.Resolver = resolver;
		this.Validator = validator;
	}

	/// <inheritdoc/>
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
