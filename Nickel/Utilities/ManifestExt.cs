using Newtonsoft.Json;
using OneOf;
using OneOf.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

/// <summary>
/// Hosts extension methods for mod manifests.
/// </summary>
public static class ManifestExt
{
	/// <summary>
	/// Attempts to convert an <see cref="IModManifest"/> to an <see cref="IAssemblyModManifest"/>.
	/// </summary>
	/// <param name="manifest">The mod manifest to convert.</param>
	/// <returns>The converted mod manifest, or an error.</returns>
	public static OneOf<IAssemblyModManifest, Error<string>> AsAssemblyModManifest(this IModManifest manifest)
	{
		if (!TryParseEntryPointAssemblyFileName(out var entryPointAssembly))
			return new Error<string>($"`{nameof(IAssemblyModManifest.EntryPointAssembly)}` value is invalid.");
		if (!TryParseEntryPointTypeFullName(out var entryPointType))
			return new Error<string>($"`{nameof(IAssemblyModManifest.EntryPointType)}` value is invalid.");
		if (!TryParseAssemblyReferences(out var assemblyReferences))
			return new Error<string>($"`{nameof(IAssemblyModManifest.AssemblyReferences)}` value is invalid.");

		return new AssemblyModManifest(manifest)
		{
			EntryPointAssembly = entryPointAssembly,
			EntryPointType = entryPointType,
			AssemblyReferences = assemblyReferences ?? []
		};

		bool TryParseEntryPointAssemblyFileName([MaybeNullWhen(false)] out string result)
		{
			result = default;
			if (!manifest.ExtensionData.TryGetValue(nameof(IAssemblyModManifest.EntryPointAssembly), out var raw))
				return false;
			if (raw is not string value)
				return false;
			result = value;
			return true;
		}

		bool TryParseEntryPointTypeFullName(out string? result)
		{
			result = null;
			if (!manifest.ExtensionData.TryGetValue(nameof(IAssemblyModManifest.EntryPointType), out var raw))
				return true;
			if (raw is not string value)
				return false;
			result = value;
			return true;
		}

		bool TryParseAssemblyReferences(out IReadOnlyList<ModAssemblyReference>? result)
		{
			var settings = new JsonSerializerSettings
			{
				ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
			};
			result = null;
			if (!manifest.ExtensionData.TryGetValue(nameof(IAssemblyModManifest.AssemblyReferences), out var raw))
				return true;
			var nullableResult = JsonConvert.DeserializeObject<List<ModAssemblyReference>>(JsonConvert.SerializeObject(raw, settings), settings);
			if (nullableResult is null)
				return false;
			result = nullableResult;
			return true;
		}
	}
}
