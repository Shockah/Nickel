using System.IO;

namespace Nickel;

internal readonly struct LaunchArguments
{
    public FileInfo? GamePath { get; init; }
    public DirectoryInfo? ModsPath { get; init; }
    public DirectoryInfo? SavePath { get; init; }
}
