using OneOf;
using OneOf.Types;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

[GenerateOneOf]
public partial class ValidatingPluginLoaderResult : OneOfBase<
	ValidatingPluginLoaderResult.Success,
	Error<string>
>
{
	public readonly struct Success
	{
		public required IReadOnlyList<string> Warnings { get; init; }
	}
}
