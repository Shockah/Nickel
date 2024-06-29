using OneOf;
using OneOf.Types;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// The result of <see cref="IPluginPackageResolver{TPluginManifest}"/>.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
[GenerateOneOf]
public partial class PluginPackageResolveResult<TPluginManifest> : OneOfBase<
	PluginPackageResolveResult<TPluginManifest>.Success,
	Error<string>
>
{
	/// <summary>
	/// The plugin package resolve succeeded.
	/// </summary>
	public readonly struct Success
	{
		/// <summary>
		/// The resolved plugin package.
		/// </summary>
		public required IPluginPackage<TPluginManifest> Package { get; init; }

		/// <summary>
		/// Any warnings that were encountered during resolving.
		/// </summary>
		public required IReadOnlyList<string> Warnings { get; init; }
	}
}
