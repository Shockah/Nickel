using Nickel.Common;
using System;
using System.Reflection;

namespace Nickel;

/** <summary>Contains various constants.</summary> */
public static class NickelConstants
{
	private static readonly Lazy<SemanticVersion> LazyVersion = new(
		() => SemanticVersionParser.TryParseForAssembly(typeof(Nickel).GetTypeInfo().Assembly, out var version)
			? version : throw new InvalidOperationException()
	);

	/** <summary>The name of the modloader.</summary> */
	public static string Name { get; } = "Nickel";
	/** <summary>The current version of the modloader.</summary> */
	public static SemanticVersion Version => LazyVersion.Value;
	/** <summary>The game version to fall back to if version parsing fails.</summary> */
	/* TODO: Why is this public...? */
	public static SemanticVersion FallbackGameVersion { get; } = new SemanticVersion(1, 0, 6);
	/** <summary>The log message to show on startup.</summary> */
	public static string IntroMessage { get; } = $"{Name} {Version} -- A modding API / modloader for the game Cobalt Core.";
	/** <summary>The default <see cref="IModManifest.ModType"/>.</summary> */
	public static string ModType { get; } = Name;
	/** <summary>The deprecated alias for the default <see cref="IModManifest.ModType"/>.</summary> */
	public static string DeprecatedModType { get; } = $"{Name}.Assembly";
}
