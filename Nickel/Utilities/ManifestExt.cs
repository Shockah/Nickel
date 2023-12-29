using System;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

public static partial class ManifestExt
{
	public static IAssemblyModManifest? AsAssemblyModManifest(this IModManifest manifest)
	{
		bool TryParseEntryPointAssemblyFileName([MaybeNullWhen(false)] out string result)
		{
			result = default;
			if (!manifest.ExtensionData.TryGetValue(nameof(IAssemblyModManifest.EntryPointAssemblyFileName), out object? raw))
				return false;
			if (raw is not string value)
				return false;
			result = value;
			return true;
		}

		bool TryParseLoadPhase([MaybeNullWhen(false)] out ModLoadPhase result)
		{
			result = ModLoadPhase.AfterGameAssembly;
			if (!manifest.ExtensionData.TryGetValue(nameof(IAssemblyModManifest.LoadPhase), out object? raw))
				return true;
			if (raw is not string stringValue)
				return false;
			if (!Enum.TryParse<ModLoadPhase>(stringValue, ignoreCase: true, out var value))
				return false;
			result = value;
			return true;
		}

		if (!TryParseEntryPointAssemblyFileName(out string? entryPointAssemblyFileName))
			return null;
		if (!TryParseLoadPhase(out var loadPhase))
			return null;
		return new AssemblyModManifest(manifest)
		{
			EntryPointAssemblyFileName = entryPointAssemblyFileName,
			LoadPhase = loadPhase
		};
	}
}
