using Newtonsoft.Json;
using OneOf;
using OneOf.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Nickel;

/// <summary>
/// Hosts extension methods for mod manifests.
/// </summary>
public static class IModManifestExtensions
{
	/// <param name="manifest">The mod manifest.</param>
	extension(IModManifest manifest)
	{
		/// <summary>
		/// Attempts to convert an <see cref="IModManifest"/> to an <see cref="IAssemblyModManifest"/>.
		/// </summary>
		/// <returns>The converted mod manifest, or an error.</returns>
		public OneOf<IAssemblyModManifest, Error<string>> AsAssemblyModManifest()
		{
			if (!TryParseEntryPointAssemblyFileName(out var entryPointAssembly))
				return new Error<string>($"`{nameof(IAssemblyModManifest.EntryPointAssembly)}` value is invalid.");
			if (!TryParseEntryPointTypeFullName(out var entryPointType))
				return new Error<string>($"`{nameof(IAssemblyModManifest.EntryPointType)}` value is invalid.");
			if (!TryParseAssemblyReferences(out var assemblyReferences))
				return new Error<string>($"`{nameof(IAssemblyModManifest.AssemblyReferences)}` value is invalid.");

			var result = AssemblyModManifest.From(manifest);
			result.EntryPointAssembly = entryPointAssembly;
			result.EntryPointType = entryPointType;
			result.AssemblyReferences = assemblyReferences ?? [];
			return result;

			bool TryParseEntryPointAssemblyFileName([MaybeNullWhen(false)] out string result)
			{
				result = null;
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

		/// <summary>
		/// Builds an as-nice-as-possible description for a mod, to be presented to the user.
		/// </summary>
		/// <param name="long">Whether to build a long description, including the mod's author(s) and the <seealso cref="IModManifest.Description"/> field.</param>
		/// <returns>The description to be presented to the user.</returns>
		public string GetDisplayName(bool @long)
		{
			var sb = new StringBuilder();
			sb.Append(string.IsNullOrEmpty(manifest.DisplayName) ? manifest.UniqueName : $"{manifest.DisplayName} ({manifest.UniqueName})");
			sb.Append($" {manifest.Version}");
			if (@long)
			{
				if (!string.IsNullOrEmpty(manifest.Author))
					sb.Append($" by {manifest.Author}");
				if (!string.IsNullOrEmpty(manifest.Description))
					sb.Append($": {manifest.Description}");
			}
			return sb.ToString();
		}
	}
}
