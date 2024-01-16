using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal readonly struct LaunchArguments
{
	public bool? Debug { get; init; }
	public bool? SaveInDebug { get; init; }
	public bool? InitSteam { get; init; }
	public FileInfo? GamePath { get; init; }
	public DirectoryInfo? ModsPath { get; init; }
	public DirectoryInfo? SavePath { get; init; }
	public DirectoryInfo? LogPath { get; init; }
	public bool? TimestampedLogFiles { get; init; }
	public string? LogPipeName { get; init; }
	public IReadOnlyList<string> UnmatchedArguments { get; init; }
}
