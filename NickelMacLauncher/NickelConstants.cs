using Nickel.Common;
using System;
using System.Reflection;

namespace Nickel.MacLauncher;

internal static class NickelConstants
{
	private static readonly Lazy<SemanticVersion> LazyVersion = new(
		() => SemanticVersionParser.TryParseForAssembly(typeof(NickelMacLauncher).GetTypeInfo().Assembly, out var version)
			? version : throw new InvalidOperationException()
	);

	public static string Name { get; } = "Nickel";
	public static SemanticVersion Version => LazyVersion.Value;
	public static string IntroMessage { get; } = $"{Name} {Version} Mac launcher -- A modding API / mod loader for the game Cobalt Core.";
}
