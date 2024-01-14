using Nickel.Common;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Nickel;

public interface IModRegistry
{
	IModManifest VanillaModManifest { get; }
	IReadOnlyDictionary<string, IModManifest> LoadedMods { get; }
	DirectoryInfo ModsDirectory { get; }

	bool TryProxy<TProxy>(object @object, [MaybeNullWhen(false)] out TProxy proxy) where TProxy : class;

	TProxy? AsProxy<TProxy>(object? @object) where TProxy : class;

	TApi? GetApi<TApi>(string uniqueName, SemanticVersion? minimumVersion = null) where TApi : class;
}
