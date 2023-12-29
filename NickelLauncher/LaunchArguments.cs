using System.Collections.Generic;
using System.IO;

namespace Nickel.Launcher;

internal readonly struct LaunchArguments
{
	public FileInfo? LaunchPath { get; init; }
	public bool? PipeLogs { get; init; }
	public IReadOnlyList<string> UnmatchedArguments { get; init; }
}
