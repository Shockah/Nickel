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
	private readonly IPluginPackageResolver<TPluginManifest> Resolver;
	private readonly Func<IPluginPackage<TPluginManifest>, Error<string>?> Validator;

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
	public IEnumerable<PluginPackageResolveResult<TPluginManifest>> ResolvePluginPackages()
		=> this.Resolver.ResolvePluginPackages().Select(resolveResult =>
		{
			if (resolveResult.TryPickT1(out var error, out var success))
				return error;
			if (this.Validator(success.Package) is { } validationError)
				return validationError;
			return (PluginPackageResolveResult<TPluginManifest>)new PluginPackageResolveResult<TPluginManifest>.Success
			{
				Package = success.Package,
				Warnings = success.Warnings
			};
		});
}
