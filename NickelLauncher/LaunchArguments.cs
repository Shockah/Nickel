using System.Collections.Generic;
using System.IO;

namespace Nickel.Launcher;

internal readonly struct LaunchArguments
{
	public FileInfo? LaunchPath { get; init; }
	public DirectoryInfo? ModStoragePath { get; init; }
	public DirectoryInfo? LogPath { get; init; }
	public bool? TimestampedLogFiles { get; init; }
	public IReadOnlyList<string> UnmatchedArguments { get; init; }
}
