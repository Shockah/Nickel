using System;
using System.Reflection;
using Nickel.Common;

namespace Nickel;

public static class NickelConstants
{
    public static string Name { get; private set; } = $"{typeof(Nickel).Namespace!}";
    public static SemanticVersion Version => LazyVersion.Value;
    public static string AssemblyModType { get; private set; } = $"{typeof(Nickel).Namespace!}.Assembly";
    public static string LegacyModType { get; private set; } = $"{typeof(Nickel).Namespace!}.Legacy";

    private static readonly Lazy<SemanticVersion> LazyVersion = new(() =>
    {
        var attribute = typeof(Nickel).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>() ?? throw new InvalidOperationException();
        if (!SemanticVersionParser.TryParse(attribute.InformationalVersion.Split("+")[0], out var version))
            throw new InvalidOperationException();
        return version;
    });
}
