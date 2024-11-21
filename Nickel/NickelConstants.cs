using Nickel.Common;
using System;
using System.Reflection;

namespace Nickel;

/// <summary>
/// Defines several constants related to the mod loader.
/// </summary>
public static class NickelConstants
{
	private static readonly Lazy<SemanticVersion> LazyVersion = new(
		() => SemanticVersionParser.TryParseForAssembly(typeof(Nickel).GetTypeInfo().Assembly, out var version)
			? version : throw new InvalidOperationException()
	);

	/// <summary>The name of the mod loader.</summary>
	public static string Name
		=> "Nickel";

	/// <summary>The current version of the mod loader.</summary>
	public static SemanticVersion Version => LazyVersion.Value;

	/// <summary>A fallback version that is used if the game's version could not be parsed.</summary>
	public static SemanticVersion FallbackGameVersion { get; } = new SemanticVersion(1, 2, 4);

	/// <summary>The intro message that is logged right when the mod loader starts.</summary>
	public static string IntroMessage { get; } = $"{Name} {Version} -- A modding API / mod loader for the game Cobalt Core.";

	/// <summary>The main mod type.</summary>
	public static string ModType { get; } = Name;

	/// <summary>A deprecated main mod type, equivalent to <see cref="ModType"/>.</summary>
	public static string DeprecatedModType { get; } = $"{Name}.Assembly";

	/// <summary>The manifest file name the mod loader scans for.</summary>
	public static string ManifestFileName
		=> "nickel.json";
}
