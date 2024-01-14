using Nickel.Common;
using System;
using System.Reflection;

namespace Nickel;

public static class NickelConstants
{
	private static readonly Lazy<SemanticVersion> LazyVersion = new(() =>
	{
		var attribute = typeof(Nickel).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>() ?? throw new InvalidOperationException();
		if (!SemanticVersionParser.TryParse(attribute.InformationalVersion.Split("+")[0], out var version))
			throw new InvalidOperationException();
		return version;
	});

	public static string Name { get; } = "Nickel";
	public static SemanticVersion Version => LazyVersion.Value;
	public static SemanticVersion FallbackGameVersion { get; } = new SemanticVersion(1, 0, 6);
	public static string IntroMessage { get; } = $"{Name} {Version} -- A modding API / modloader for the game Cobalt Core.";
	public static string ModType { get; } = Name;
	public static string DeprecatedModType { get; } = $"{Name}.Assembly";
}
