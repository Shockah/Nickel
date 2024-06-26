using OneOf;
using OneOf.Types;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// The result of <see cref="IPluginLoader{TPluginManifest,TPlugin}"/>.
/// </summary>
/// <typeparam name="TPlugin">The plugin type.</typeparam>
[GenerateOneOf]
public partial class PluginLoadResult<TPlugin> : OneOfBase<
	PluginLoadResult<TPlugin>.Success,
	Error<string>
>
{
	/// <summary>
	/// The plugin load succeeded.
	/// </summary>
	public readonly struct Success
	{
		/// <summary>
		/// The loaded plugin.
		/// </summary>
		public required TPlugin Plugin { get; init; }
		
		/// <summary>
		/// Any warnings that were encountered during the loading of this plugin.
		/// </summary>
		public required IReadOnlyList<string> Warnings { get; init; }
	}
}
