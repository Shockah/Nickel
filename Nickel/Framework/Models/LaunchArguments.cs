using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal readonly struct LaunchArguments
{
    public bool? Debug { get; init; }
    public FileInfo? GamePath { get; init; }
    public DirectoryInfo? ModsPath { get; init; }
    public DirectoryInfo? SavePath { get; init; }
    public IReadOnlyList<string> UnmatchedParameters { get; init; }
}
