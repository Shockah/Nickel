using System.Collections.Generic;
using System.IO;

namespace Nickel.LinuxLauncher;

internal readonly struct LaunchArguments
{
	public bool SkipLauncher { get; init; }
	public FileInfo? ExecutablePath { get; init; }
	public IReadOnlyList<string> UnmatchedArguments { get; init; }
}
