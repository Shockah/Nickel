using OneOf;
using OneOf.Types;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

public static partial class ManifestExt
{
	public static OneOf<IAssemblyModManifest, Error<string>> AsAssemblyModManifest(this IModManifest manifest)
	{
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

		bool TryParseLoadPhase([MaybeNullWhen(false)] out ModLoadPhase result)
		{
			result = ModLoadPhase.AfterGameAssembly;
			if (!manifest.ExtensionData.TryGetValue(nameof(IAssemblyModManifest.LoadPhase), out var raw))
				return true;
			if (raw is not string stringValue)
				return false;
			if (!Enum.TryParse<ModLoadPhase>(stringValue, ignoreCase: true, out var value))
				return false;
			result = value;
			return true;
		}

		if (!TryParseEntryPointAssemblyFileName(out var entryPointAssembly))
			return new Error<string>($"`{nameof(IAssemblyModManifest.EntryPointAssembly)}` value is invalid.");
		if (!TryParseEntryPointTypeFullName(out var entryPointType))
			return new Error<string>($"`{nameof(IAssemblyModManifest.EntryPointType)}` value is invalid.");
		if (!TryParseLoadPhase(out var loadPhase))
			return new Error<string>($"`{nameof(IAssemblyModManifest.LoadPhase)}` value is invalid.");

		return new AssemblyModManifest(manifest)
		{
			EntryPointAssembly = entryPointAssembly,
			EntryPointType = entryPointType,
			LoadPhase = loadPhase
		};
	}
}
