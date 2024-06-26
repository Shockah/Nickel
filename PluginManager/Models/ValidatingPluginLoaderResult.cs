using OneOf;
using OneOf.Types;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// A result of <see cref="ValidatingPluginLoader{TPluginManifest,TPlugin}"/>.
/// </summary>
[GenerateOneOf]
public partial class ValidatingPluginLoaderResult : OneOfBase<
	ValidatingPluginLoaderResult.Success,
	Error<string>
>
{
	/// <summary>
	/// Plugin validation succeeded.
	/// </summary>
	public readonly struct Success
	{
		/// <summary>
		/// Any warnings that were encountered during validation of this plugin.
		/// </summary>
		public required IReadOnlyList<string> Warnings { get; init; }
	}
}
