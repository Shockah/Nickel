using System;
using System.Reflection;
using Nickel.Common;

namespace Nickel.Launcher;

internal static class NickelConstants
{
	private static readonly Lazy<SemanticVersion> LazyVersion = new(() =>
	{
		var attribute = typeof(NickelLauncher).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>() ?? throw new InvalidOperationException();
		if (!SemanticVersionParser.TryParse(attribute.InformationalVersion.Split("+")[0], out var version))
			throw new InvalidOperationException();
		return version;
	});

	public static string Name { get; private set; } = "Nickel";
	public static SemanticVersion Version => LazyVersion.Value;
	public static string IntroMessage { get; private set; } = $"{Name} {Version} launcher -- A modding API / modloader for the game Cobalt Core.";
}
