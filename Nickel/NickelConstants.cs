using System;
using System.Reflection;
using Nickel.Common;

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

	public static string Name { get; private set; } = "Nickel";
	public static SemanticVersion Version => LazyVersion.Value;
	public static string IntroMessage { get; private set; } = $"{Name} {Version} -- A modding API / modloader for the game Cobalt Core.";
	public static string AssemblyModType { get; private set; } = $"{typeof(Nickel).Namespace!}.Assembly";
	public static string LegacyModType { get; private set; } = $"{typeof(Nickel).Namespace!}.Legacy";
}
