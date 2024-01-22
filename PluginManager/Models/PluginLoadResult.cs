using OneOf;
using OneOf.Types;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

[GenerateOneOf]
public partial class PluginLoadResult<TPlugin> : OneOfBase<
	PluginLoadResult<TPlugin>.Success,
	Error<string>
>
{
	public readonly struct Success
	{
		public required TPlugin Plugin { get; init; }
		public required IReadOnlyList<string> Warnings { get; init; }
	}
}
